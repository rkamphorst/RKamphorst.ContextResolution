using System.Net;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.HttpApi.Dto;

namespace RKamphorst.ContextResolution.HttpApi;

public class HttpApiWithContext<TRequest, TResult> : IHttpApiWithContext<TRequest, TResult>
{
    private readonly Func<HttpClient> _createHttpClient;
    private readonly Uri _uri;
    private readonly ILogger _logger;

    public static HttpApiWithContext<TRequest, TResult> Create(
        IHttpClientFactory httpClientFactory, string httpClientName, Uri uri, ILogger logger) =>
        new(
            () => httpClientFactory.CreateClient(httpClientName), uri, logger);
    
    public HttpApiWithContext(Func<HttpClient> createHttpClient, Uri uri, ILogger logger)
    {
        _createHttpClient = createHttpClient;
        _uri = uri;
        _logger = logger;
    }

    public async Task<TResult> PostAsync(
        TRequest request,
        IContextProvider contextProvider, CancellationToken cancellationToken)
    {
        var context = new Dictionary<ContextKey, object>();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var content = new StringContent(JsonConvert.SerializeObject(
                new RequestWithContextDto<TRequest>
                {
                    Request = request,
                    Context = context.ToDictionary(
                        kvp => kvp.Key.Key, 
                        kvp => kvp.Value
                        )
                }, Formatting.None), Encoding.UTF8, MediaTypeNames.Application.Json);

            (NeedContextDto? needContext, TResult? successResult) =
                await PostInternalAsync(content, context, cancellationToken);

            if (successResult != null)
            {
                return successResult;
            }

            // either result is null or needContext is null, so if we arrive here,
            // needContext is not null

            (ContextKey key, object)[] fetchedContexts =
                await Task.WhenAll(
                    needContext!.GetRequestedContextKeys().Select(async key => (key,
                        await contextProvider.GetContextAsync(
                            key.Name.Key, key.Id, false, cancellationToken
                        ))).Concat(

                        needContext.GetRequiredContextKeys().Select(async key => (key,
                            await contextProvider.GetContextAsync(
                                key.Name.Key, key.Id, true, cancellationToken
                            )))
                    )
                );

            foreach ((ContextKey key, object fetchedContext) in fetchedContexts)
            {
                context.Add(key, fetchedContext);
            }
        }
    }

    private async Task<(NeedContextDto?, TResult?)> PostInternalAsync(
        HttpContent content, Dictionary<ContextKey, object> context,
            CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _createHttpClient().PostAsync(_uri, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            TResult result = await HandleSuccessResponseAsync(response, cancellationToken);
            _logger.LogDebug(
                "POST to {Uri} was successful with result {@Result}",
                _uri, result);
            return (default, result);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            NeedContextDto needContext = await HandleBadRequestResponseAsync(response, context, cancellationToken);
            _logger.LogDebug(
                "POST to {Uri} resulted in request for context(s) {@NeedContext}",
                _uri, needContext);
            return (needContext, default);
        }

        _logger.LogWarning(
            "Post to {Uri} failed with status {HttpStatusCode}",
            _uri, response.StatusCode);
        throw new HttpRequestException($"Status {response.StatusCode}: {response.ReasonPhrase}", null,
            response.StatusCode);
    }

    private async Task<NeedContextDto> HandleBadRequestResponseAsync(
        HttpResponseMessage response, Dictionary<ContextKey, object> context, CancellationToken cancellationToken)
    {
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var sr = new StreamReader(stream);
        var stringContent = await sr.ReadToEndAsync();

        NeedContextDto? needContextResponse;
        try
        {
            needContextResponse = JsonConvert.DeserializeObject<NeedContextDto>(stringContent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Response with status {HttpStatusCode} " +
                $"could not be parsed as {nameof(NeedContextDto)}, throwing exception", response.StatusCode
            );
            throw new HttpRequestException(
                $"Status {response.StatusCode}, " +
                $"response could not be parsed as {nameof(NeedContextDto)}",
                ex, response.StatusCode
            );
        }

        if (true != needContextResponse?.IsValid)
        {
            _logger.LogWarning( 
                "Response with status {HttpStatusCode} " +
                $"could not be parsed as {nameof(NeedContextDto)}, throwing exception", response.StatusCode
            );
            throw new HttpRequestException(
                $"Status {response.StatusCode}, " +
                $"response could not be parsed as {nameof(NeedContextDto)}", null, HttpStatusCode.BadRequest
            );
        }

        var allRequestedKeys =
            needContextResponse.GetRequestedContextKeys()
                .Concat(needContextResponse.GetRequiredContextKeys())
                .Distinct().ToArray();
        
        ContextKey[] allMissingKeys =
            allRequestedKeys
                .Where(key => !context.ContainsKey(key))
                .ToArray();

        if (allMissingKeys.Length == 0)
        {
            _logger.LogWarning(
                "Response with status {HttpStatusCode}, " +
                $"{nameof(needContextResponse.RequestContext)} has only context references " +
                "for contexts that were already submitted: {@NeedContextStrings}, throwing exception",
                response.StatusCode, 
                allRequestedKeys.Select(k => k.Key).ToArray());

            throw new HttpRequestException(
                $"Status {response.StatusCode}, " +
                $"{nameof(needContextResponse.RequestContext)} has only context references " +
                "for contexts that were already submitted: " +
                $"{string.Join(",", allRequestedKeys.Select(k => k.Key).ToArray())}",
                null, response.StatusCode);
        }

        return needContextResponse;
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