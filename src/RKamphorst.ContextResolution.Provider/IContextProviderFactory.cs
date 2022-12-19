using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

/// <summary>
/// Creates context providers, given a parameter
/// </summary>
/// <typeparam name="TParameter">Type of the parameter to get context for</typeparam>
public interface IContextProviderFactory
{
    /// <summary>
    /// Create a context provider
    /// </summary>
    /// <param name="parameter">Parameter to create the context provider for</param>
    /// <typeparam name="TParameter">Type of the parameter to create the context provider for</typeparam>
    public IContextProvider CreateContextProvider<TParameter>(TParameter parameter);
}