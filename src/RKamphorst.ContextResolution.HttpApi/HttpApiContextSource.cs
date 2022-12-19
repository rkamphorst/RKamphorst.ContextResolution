using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.HttpApi;

public class HttpApiContextSource<TParameter> : IContextSource<TParameter, JObject>
{
    private readonly IHttpapiWithContext<TParameter, JObject> _httpapiWithContext;

    public const string DefaultHttpClientName = "HttpApiContextSource";

    public HttpApiContextSource(IHttpClientFactory httpClientFactory, ILogger<HttpApiContextSource<TParameter>> logger)
        : this(httpClientFactory, DefaultHttpClientName, logger)
    {
    }

    protected HttpApiContextSource(IHttpClientFactory httpClientFactory, string httpClientName,
        ILogger<HttpApiContextSource<TParameter>> logger)
    {
        _httpapiWithContext =
            HttpApiWithContext<TParameter, JObject>.Create(httpClientFactory, httpClientName, logger);
    }

    public async Task FillContextAsync(JObject contextToFill, TParameter parameter, string? key, IContextProvider contextProvider, CancellationToken cancellationToken = default)
    {
        var result = await _httpapiWithContext.PostAsync(
            new Uri(key ?? ""),
            parameter, 
            contextProvider,
            cancellationToken
        );

        contextToFill.Merge(result, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Concat,
            MergeNullValueHandling = MergeNullValueHandling.Merge,
            PropertyNameComparison = StringComparison.InvariantCulture
        });
    }

}