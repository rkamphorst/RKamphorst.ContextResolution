using System.Net;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.HttpApi.Dto;

namespace RKamphorst.ContextResolution.HttpApi;

public class HttpApiWithContext<TParameter, TResult> : IHttpapiWithContext<TParameter, TResult>
{
    public const string DefaultHttpClientName = "HttpApiWithContext";

    public static IHttpapiWithContext<TParameter, TResult> Create<TComponent>(IHttpClientFactory httpClientFactory,
        string? httpClientName,
        ILogger<TComponent> logger) =>
        new HttpApiWithContext<TParameter, TResult>(httpClientFactory, logger,
            httpClientName ?? typeof(TComponent).Name);

    protected readonly string HttpClientName;
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public HttpApiWithContext(IHttpClientFactory httpClientFactory,
        ILogger<HttpApiWithContext<TParameter, TResult>> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        HttpClientName = DefaultHttpClientName;
    }

    private HttpApiWithContext(IHttpClientFactory httpClientFactory,
        ILogger logger, string httpClientName)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        HttpClientName = httpClientName;
    }

    public async Task<TResult> PostAsync(Uri requestUri,
        TParameter parameter,
        IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        var context = new Dictionary<string, object>();
        
        while (!cancellationToken.IsCancellationRequested)
        {

            var content = new StringContent(JsonConvert.SerializeObject(
                new RequestWithContext<TParameter>
                {
                    Parameter = parameter,
                    Context = context
                }, Formatting.None), Encoding.UTF8, MediaTypeNames.Application.Json);

            (ContextAndKey[]? needContext, TResult? result) =
                await PostInternalAsync(requestUri, content, context, cancellationToken);

            if (result != null)
            {
                return result;
            }

            // either result is null or needContext is null, so if we arrive here,
            // needContext is not null

            var fetchedContexts =
                await Task.WhenAll(
                    needContext!.Select(async cr =>
                    {
                        var dictionaryKey = cr.ParsedFromString;
                        var contextName = cr.ContextName;
                        var key = cr.Key;
                        return (dictionaryKey,
                            await contextProvider.GetContextAsync(contextName, key, cancellationToken));
                    }));

            foreach (var (dictionaryKey, fetchedContext) in fetchedContexts)
            {
                context.Add(dictionaryKey, fetchedContext);
            }
        }

        throw new OperationCanceledException();
    }

    private async Task<(ContextAndKey[]? NeedContext, TResult? Result)>
        PostInternalAsync(Uri requestUri, HttpContent content, Dictionary<string, object> context,
            CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var response = await client.PostAsync(requestUri, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = (
                NeedContext: (ContextAndKey[]?)null,
                Result: await HandleSuccessResponseAsync(response, cancellationToken)
            );
            _logger.LogInformation(
                "POST to {Uri} was successful with result {@Result}",
                requestUri, result.Result);
            return result;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var result = (
                NeedContext: await HandleBadRequestResponseAsync(response, context, cancellationToken),
                Result: default(TResult)
            );
            _logger.LogInformation(
                "POST to {Uri} resulted in request for context(s) {@NeedContext}",
                requestUri, result.NeedContext);
            return result;
        }

        _logger.LogWarning(
            "Post to {Uri} failed with status {HttpStatusCode}",
            requestUri, response.StatusCode);
        throw new HttpRequestException($"Status {response.StatusCode}: {response.ReasonPhrase}", null,
            response.StatusCode);
    }

    /// <summary>
    /// Handles a bad request
    /// </summary>
    /// <remarks>
    /// If a bad request response meets the following requirements:
    ///
    /// 1. The response can be json-deserialized into <see cref="NeedContextResponse"/>
    /// 2. Property <see cref="NeedContextResponse.NeedContext"/> contains a non-empty list of strings
    /// 3. All these strings are valid context references (<see cref="ContextAndKey"/>)
    /// 4. At least one of these context references was not in the already submitted <paramref name="context"/>
    ///
    /// Then a list of <see cref="ContextAndKey"/> is returned: references to requested context.
    ///
    /// In all other cases, a <see cref="HttpRequestException"/> is thrown with the original status and reason phrase,
    /// and additionally an explanation why the <see cref="NeedContextResponse"/> was not valid. 
    /// </remarks>
    /// <param name="response">Response to parse</param>
    /// <param name="context">Already submitted context dictionary</param>
    /// <param name="cancellationToken">Cancellation support</param>
    /// <returns></returns>
    private async Task<ContextAndKey[]> HandleBadRequestResponseAsync(
        HttpResponseMessage response, Dictionary<string, object> context, CancellationToken cancellationToken)
    {
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var sr = new StreamReader(stream);
        var stringContent = await sr.ReadToEndAsync();

        NeedContextResponse? needContextResponse;
        try
        {
            needContextResponse = JsonConvert.DeserializeObject<NeedContextResponse>(stringContent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Response with status {HttpStatusCode} " +
                $"could not be parsed as {nameof(NeedContextResponse)}, throwing exception", response.StatusCode
            );
            throw new HttpRequestException(
                $"Status {response.StatusCode}, " +
                $"response could not be parsed as {nameof(NeedContextResponse)}",
                ex, response.StatusCode
            );
        }

        if (!needContextResponse!.IsValid)
        {
            var badContextStrings = needContextResponse.GetBadContextReferenceStrings();
            _logger.LogWarning(
                "Response with status {HttpStatusCode}, " +
                $"{nameof(needContextResponse.NeedContext)} has bad " +
                "context reference strings: {@NeedContextStrings}, throwing exception",
                response.StatusCode, badContextStrings);
            throw new HttpRequestException(
                $"Status {response.StatusCode}, " +
                $"{nameof(needContextResponse.NeedContext)} has bad " +
                $"context reference strings: {string.Join(", ", badContextStrings)}",
                null, response.StatusCode);
        }

        var allContextReferences = needContextResponse.GetContextReferences().ToArray();
        
        var result = allContextReferences
            .Where(cr => !context.ContainsKey(cr.ParsedFromString))
            .ToArray();

        if (result.Length == 0)
        {
            _logger.LogWarning(
                "Response with status {HttpStatusCode}, " +
                $"{nameof(needContextResponse.NeedContext)} has only context references " +
                "for contexts that were already submitted: {@NeedContextStrings}, throwing exception",
                response.StatusCode, 
                allContextReferences.Select(cr => cr.ParsedFromString).ToArray());

            throw new HttpRequestException(
                $"Status {response.StatusCode}, " +
                $"{nameof(needContextResponse.NeedContext)} has only context references " +
                "for contexts that were already submitted: " +
                $"{string.Join(",", allContextReferences.Select(cr => cr.ParsedFromString).ToArray())}",
                null, response.StatusCode);
        }

        return result;
    }

    private async Task<TResult> HandleSuccessResponseAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var sr = new StreamReader(stream);
        var stringContent = await sr.ReadToEndAsync();

        var result = JsonConvert.DeserializeObject<TResult>(stringContent);
        return result!;
    }
}