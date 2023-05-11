using Microsoft.Extensions.Logging;
using RKamphorst.ContextResolution.Contract;
using RKamphorst.ContextResolution.HttpApi.Dto;

namespace RKamphorst.ContextResolution.HttpApi;

/// <summary>
/// Named context source that gets context from an HTTP api
/// </summary>
public class HttpApiNamedContextSource : INamedContextSource
{
    public const string HttpClientName = nameof(HttpApiNamedContextSource);
    
    private readonly IHttpApiWithContext<ContextKeyDto, ContextResultsDto> _httpApiWithContext;
  
    public HttpApiNamedContextSource(
        IHttpClientFactory httpClientFactory, Uri uri, ILogger logger)
    {
        _httpApiWithContext = 
            HttpApiWithContext<ContextKeyDto, ContextResultsDto>.Create(httpClientFactory, HttpClientName, uri, logger);
    }

    public async Task<ContextResult[]> GetContextAsync(ContextKey key, IContextProvider contextProvider,
        CancellationToken cancellationToken)
    {
        var contextKeyDto = new ContextKeyDto { ContextKey = key.Key };
        ContextResultsDto results =
            await _httpApiWithContext.PostAsync(contextKeyDto, contextProvider, cancellationToken);
        return (results.Results ?? Array.Empty<ContextResultDto>())
            .Select(
                r => ContextResult.Success(
                    (string) key.Name, r.Result ?? new { },
                    r.CacheInstruction == null ? CacheInstruction.Transient : (CacheInstruction)r.CacheInstruction
                )
            ).ToArray();
    }
}