namespace RKamphorst.ContextResolution.Contract;

/// <summary>
/// Name a context class
/// </summary>
/// <remarks>
/// Any class with an empty constructor can be used as a context.
///
/// By default a context's name is the same as its class name, either the simple class name or the fully classified one.
/// With this attribute, you can define additional names under which a class can be found as a context.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ContextNameAttribute : Attribute
{
    /// <summary>
    /// The name to define for the context
    /// </summary>
    public string Name { get; }

    public ContextNameAttribute(string name)
    {
        Name = name;
    }
}