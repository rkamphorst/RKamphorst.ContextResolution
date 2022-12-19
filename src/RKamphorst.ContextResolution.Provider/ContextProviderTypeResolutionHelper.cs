using System.Reflection;
using RKamphorst.ContextResolution.Contract;

namespace RKamphorst.ContextResolution.Provider;

public static class ContextProviderTypeResolutionHelper
{
    private static readonly Dictionary<string, IContextProviderInvoker> ContextProviderInvokers = new();

    private interface IContextProviderInvoker
    {
        Task<object> GetContextAsync(IContextProvider contextProvider, string? key = null,
            CancellationToken cancellationToken = default);
    }

    private class ContextProviderInvoker<TContext> : IContextProviderInvoker
        where TContext : class, new()
    {
        public async Task<object> GetContextAsync(IContextProvider contextProvider, string? key = null,
            CancellationToken cancellationToken = default)
        {
            return await contextProvider.GetContextAsync<TContext>(key, cancellationToken);
        }
    }

    private static Lazy<IReadOnlyDictionary<string, Type[]>> ContextTypeMap
    {
        get
        {
            
            
            return new(()
                =>
            {
                var allAssemblies = new[] { Assembly.GetEntryAssembly() }
                    .Concat(AppDomain.CurrentDomain.GetAssemblies())
                    .Concat(Assembly.GetEntryAssembly()?.GetReferencedAssemblies().Select(Assembly.Load) ??
                            Enumerable.Empty<Assembly>()).Distinct().ToArray();
                var allPossibleContextTypes = // get all the assemblies
                    allAssemblies
                        .SelectMany(assembly =>
                            assembly?.DefinedTypes.Select(i => i.AsType())
                            ?? Array.Empty<Type>()
                        ).Where(CanBeContext).ToArray();
                
                var typeMap =
                    allPossibleContextTypes // keep only types that are valid contexts 
                        .SelectMany(type =>
                            GetContextNamesForType(type).Select(name => (Name: name, Type: type))
                        ) // make a big list of full names and short names for each type
                        .Where(t => !string.IsNullOrWhiteSpace(t.Name)) // discard types that have no suitable name
                        .Distinct() // make sure we have only unique tuples
                        .GroupBy(t => t.Name) // now, some names may have the same name, so we group them
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(t => t.Type).ToArray());

                return typeMap;
            });
            // and finally we create a dictionary name => type[]
        }
    }

    private static bool CanBeContext(Type type)
    {
        // this corresponds to 'where TContext: class, new()' 
        return type.IsClass && !type.IsAbstract && type.IsPublic && type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static IEnumerable<string> GetContextNamesForType(Type type)
    {
        if (!string.IsNullOrWhiteSpace(type.FullName))
        {
            yield return type.FullName;
            yield return type.FullName[(type.FullName.LastIndexOf('.') + 1)..];
        }

        foreach (var att in type.GetCustomAttributes<ContextNameAttribute>())
        {
            yield return att.Name;
        }
    }

    private static Type GetUniqueContextTypeForName(string name)
    {
        if (ContextTypeMap.Value.TryGetValue(name, out var results) && results.Length > 0)
        {
            if (results.Length == 1)
            {
                return results[0];
            }

            throw new ContextNameAmbiguousException(name, results);
        }

        throw new ContextNameNotFoundException(name);
    }

    private static IContextProviderInvoker GetOrCreateContextProviderInvoker(string typeName)
    {
        if (!ContextProviderInvokers.TryGetValue(typeName, out var invoker))
        {
            invoker =
                (IContextProviderInvoker)Activator.CreateInstance(
                    typeof(ContextProviderInvoker<>).MakeGenericType(GetUniqueContextTypeForName(typeName))
                )!;
            ContextProviderInvokers[typeName] = invoker;
        }

        return invoker;
    }


    public static Task<object> GetContextAsync(IContextProvider contextProvider, string typeName, string? key = null,
        CancellationToken cancellationToken = default)
    {
        return GetOrCreateContextProviderInvoker(typeName).GetContextAsync(contextProvider, key, cancellationToken);
    }
}