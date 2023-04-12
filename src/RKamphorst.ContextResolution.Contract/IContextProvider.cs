namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Provides context of type <typeparamref name="TContext"/>
/// </summary>
/// </typeparam>
public interface IContextProvider
{
    /// <summary>
    /// Gets a context of a certain type 
    /// </summary>
    /// <remarks>
    /// The type <typeparamref name="TContext"/> should have of one or more "identity" properties
    /// (e.g. a property "ID"), and remaining properties that represent the information corresponding to that identity.
    ///
    /// This method will attempt to resolve <paramref name="requestedContext"/>, which (presumably) has its identity
    /// properties set. It it will then return a <typeparamref name="TContext"/> with additional information properties
    /// set.
    ///
    /// It does so by finding appropriate context sources. There are two kinds of context sources:
    /// 
    /// - <see cref="INamedContextSource"/>
    ///   Named context sources are resolved with the name of the context, which is derived from
    ///   <typeparamref name="TContext"/>. See <see cref="ContextName"/> for more information.    
    ///   Named context sources provide more flexibility than typed context sources to attach context sources from
    ///   other environments (e.g. python).
    /// 
    /// - <see cref="ITypedContextSource{TContext}"/>
    ///   Typed context sources are resolved with the type <typeparamref name="TContext"/>.
    ///   Typed context sources provide better performance than named context sources. 
    /// 
    /// The results provided by all appropriate context sources are combined and returned as the resulting
    /// <typeparamref name="TContext"/>. 
    ///
    /// Example: in an operation that involves a person (with an ID), we request the names of the pets they own
    /// (the context). The context type could be <c>PersonPets</c>, which has an identity <c>PersonId</c> and
    /// information property <c>PetNames</c>:
    ///
    /// <code>
    /// class PersonPets {
    ///    string PersonId { get; init; }
    ///    string[] PetNames { get; set; }
    /// }
    /// </code>
    ///
    /// A call to this method would look like this:
    ///
    /// <code>
    /// var pets = await GetContextAsync(new PersonPets { PersonId = "jake" }, cancellationToken);
    /// 
    /// /*
    ///   pets == PersonPets {
    ///     PersonId: "jake",
    ///     PetNames: [ "vicky", "boris", "rex" ]
    ///   }
    /// */
    /// </code>
    ///  
    /// </remarks>
    /// <param name="requestedContext">
    ///     Identity of the context to provide.
    /// </param>
    /// <param name="requireAtLeastOneSource">
    ///     If set to true, throw a ContextSourceNotFoundException if no source was found for this context.
    /// </param>
    /// <exception cref="ContextSourceNotFoundException">
    ///     Thrown if no context sources are associated with <typeparamref name="TContext"/>
    /// </exception>
    /// <exception cref="ContextCircularDependencyException">
    ///     Thrown if the dependency chain of context sources invoked contains a circular dependency.
    /// </exception>
    /// <typeparam name="TContext">
    ///     Type of context to provide
    /// </typeparam>
    public Task<TContext> GetContextAsync<TContext>(TContext? requestedContext = null, bool requireAtLeastOneSource = false,
        CancellationToken cancellationToken = default)
        where TContext : class, new();

    /// <summary>
    /// Gets a context of a context type with a given name
    /// </summary>
    /// <remarks>
    /// This is a variation on <see cref="GetContextAsync{TContext}"/>. This method does the same, but it finds the
    /// context sources by the given <paramref name="contextName"/> instead of the context type
    /// (there is no TContext type parameter here).
    ///
    /// The *name* of a context is one of the following:
    /// - A context type's simple type name
    /// - A context type's full type name
    /// - Any name provided by the <see cref="ContextNameAttribute"/> on a context type
    /// - Any name that is supported by a <see cref="INamedContextSource"/>
    ///
    /// Example: in an operation that involves a person (with an ID), we need as context the names of the pets they own.
    /// The context type is PersonPets, which has an "identifying part" PersonId and the context PetNames:
    ///
    /// <code>
    /// JObject personPets = JObject.FromObject(new {
    ///    PersonId = "jake"
    /// }
    /// </code>
    ///
    /// A call to this method look like this:
    ///
    /// <code>
    /// var pets = await GetContextAsync(new PersonPets { PersonId = "jake" }, cancellationToken);
    /// 
    /// /*
    ///   pets == PersonPets {
    ///     PersonId: "jake",
    ///     PetNames: [ "vicky", "boris", "rex" ]
    ///   }
    /// */
    /// </code>
    /// </remarks>
    /// <param name="contextName">Name of the context type</param>
    /// <param name="requestedContext">Identifying part of the context to provide.</param>
    /// <param name="requireAtLeastOneSource">If set to true, throw a ContextSourceNotFoundException if no source was found for this context</param>
    /// <exception cref="ContextNameNotFoundException">
    /// Thrown if no context type could be found with name <paramref name="contextName"/>
    /// </exception>
    /// <exception cref="ContextNameAmbiguousException">
    /// Thrown if more than one context type was found with name <paramref name="contextName"/>
    /// </exception>
    /// <exception cref="ContextSourceNotFoundException">
    /// Thrown if no context sources are associated with the context type referred to by <paramref name="contextName"/>
    /// </exception>
    /// <exception cref="ContextCircularDependencyException">
    /// Thrown if the dependency chain of context sources invoked contains a circular dependency.
    /// </exception>
    /// <typeparam name="TContext">Type of context to provide</typeparam>
    public Task<object> GetContextAsync(string contextName, object? requestedContext,
        bool requireAtLeastOneSource = false, CancellationToken cancellationToken = default);
}

