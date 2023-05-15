using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.HttpApi;

public interface IHttpApiWithContext<in TRequest, TResult>
{
    Task<TResult> PostAsync(
        TRequest request,
        IContextProvider contextProvider, CancellationToken cancellationToken);
}