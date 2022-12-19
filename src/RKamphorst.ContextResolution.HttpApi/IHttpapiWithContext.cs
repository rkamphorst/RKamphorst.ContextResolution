using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.HttpApi;

public interface IHttpapiWithContext<in TParameter, TResult>
{
    Task<TResult> PostAsync(Uri requestUri,
        TParameter parameter,
        IContextProvider contextProvider, CancellationToken cancellationToken);
}