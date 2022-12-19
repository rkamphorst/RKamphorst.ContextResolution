namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Provides context for a parameter of type <typeparamref name="TParameter"/>
/// </summary>
/// </typeparam>
public interface IContextProvider
{
    /// <summary>
    /// Gets a context of a certain type that has a parameterless constructor
    /// </summary>
    /// <remarks>
    /// The context is created and then offered to context sources to fill.
    /// Hence the need for a parameterless constructor.
    /// </remarks>
    /// <param name="key"></param>
    /// <exception cref="ContextSourceNotFoundException">
    /// Thrown if no context sources are associated with <typeparamref name="TContext"/>
    /// </exception>
    /// <exception cref="ContextCircularDependencyException">
    /// Thrown if the dependency chain of context sources invoked contains a circular dependency.
    /// </exception>
    /// <typeparam name="TContext">Type of context to provide</typeparam>
    public Task<TContext> GetContextAsync<TContext>(string? key = null, CancellationToken cancellationToken = default)
        where TContext : class, new();
    
    /// <summary>
    /// Gets a context of a certain type, providing a factory to create the context
    /// </summary>
    /// <remarks>
    /// The context is created and then offered to context sources to fill.
    /// Hence the need for a factory method.
    /// </remarks>
    /// <param name="key"></param>
    /// <exception cref="ContextSourceNotFoundException">
    /// Thrown if no context sources are associated with <typeparamref name="TContext"/>
    /// </exception>
    /// <exception cref="ContextCircularDependencyException">
    /// Thrown if the dependency chain of context sources invoked contains a circular dependency.
    /// </exception>
    /// <typeparam name="TContext">Type of context to provide</typeparam>
    public Task<TContext> GetContextAsync<TContext>(Func<TContext> createNewContext, string? key = null, CancellationToken cancellationToken = default)
        where TContext : class;

    /// <summary>
    /// Gets a context of a context type with a given name
    /// </summary>
    /// <remarks>
    /// This looks up the type of a context source by its name. This can be the simple type name, the full name, or
    /// any name provided by the <see cref="ContextNameAttribute"/> on the context type.
    /// </remarks>
    /// <param name="typeName">Name of the context type</param>
    /// <param name="key">Optional key to provide to the context source</param>
    /// <exception cref="ContextNameNotFoundException">
    /// Thrown if no context type could be found with name <paramref name="typeName"/>
    /// </exception>
    /// <exception cref="ContextNameAmbiguousException">
    /// Thrown if more than one context type was found with name <paramref name="typeName"/>
    /// </exception>
    /// <exception cref="ContextSourceNotFoundException">
    /// Thrown if no context sources are associated with the context type referred to by <paramref name="typeName"/>
    /// </exception>
    /// <exception cref="ContextCircularDependencyException">
    /// Thrown if the dependency chain of context sources invoked contains a circular dependency.
    /// </exception>
    /// <typeparam name="TContext">Type of context to provide</typeparam>
    public Task<object> GetContextAsync(string typeName, string? key = null, CancellationToken cancellationToken = default);
}

