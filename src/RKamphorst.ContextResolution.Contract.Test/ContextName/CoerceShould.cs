using FluentAssertions;
using Newtonsoft.Json.Linq;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextName;

using ContextName = Contract.ContextName;

public class CoerceShould
{
    [Fact]
    public void ConvertJObjectToContextNameType()
    {
        var result = ((ContextName)"alias-1").Coerce(new JObject() { ["property"] = "my value" });

        result.Should().BeOfType<StubContextWithAliases>();
        result.Should().BeEquivalentTo(new StubContextWithAliases { Property = "my value" });
    }
    
    [Fact]
    public void ConvertAnonymousTypeToContextNameType()
    {
        var result = ((ContextName)"alias-1").Coerce(new { property = "my value" });

        result.Should().BeOfType<StubContextWithAliases>();
        result.Should().BeEquivalentTo(new StubContextWithAliases { Property = "my value" });
    }

    [Fact]
    public void ConvertOtherTypeToContextNameType()
    {
        var result = ((ContextName)"alias-1").Coerce(new StubContextWithAliases2 { Property = "my value" });

        result.Should().BeOfType<StubContextWithAliases>();
        result.Should().BeEquivalentTo(new StubContextWithAliases { Property = "my value" });
    }

    [Fact]
    public void ConvertNullToContextNameType()
    {
        var result = ((ContextName)"alias-1").Coerce(null);

        result.Should().BeOfType<StubContextWithAliases>();
        result.Should().BeEquivalentTo(new StubContextWithAliases { Property = null });
    }
    
    [Fact]
    public void NotConvertContextNameType()
    {
        var result = ((ContextName)"alias-1").Coerce(new StubContextWithAliases { Property = "my value" });

        result.Should().BeOfType<StubContextWithAliases>();
        result.Should().BeEquivalentTo(new StubContextWithAliases { Property = "my value" });
    }

    [Fact]
    public void ThrowIfAttemptingToConvertArrayToContextNameType()
    {
        ((Func<object>)(() => ((ContextName)"alias-1").Coerce(new[] { new { } }))).Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void ThrowIfAttemptingToConvertStringToContextNameType()
    {
        ((Func<object>)(() => ((ContextName)"alias-1").Coerce("strings"))).Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void NotConvertStrongTypeIfContextNameHasNoType()
    {
        var result = ((ContextName)"named-context").Coerce(new StubContextWithAliases { Property = "my value" });

        result.Should().BeOfType<StubContextWithAliases>();
        result.Should().BeEquivalentTo(new StubContextWithAliases { Property = "my value" });
    }
    
    [Fact]
    public void NotConvertAnonymousTypeIfContextNameHasNoType()
    {
        var result = ((ContextName)"named-context").Coerce(new { property = "my value" });
        result.Should().BeEquivalentTo(new { property = "my value" });
    }
    
    [Fact]
    public void ConvertNullIfContextNameHasNoType()
    {
        var result = ((ContextName)"named-context").Coerce(null);
        result.Should().NotBeNull();
    }
    
    [Fact]
    public void ThrowIfAttemptingToConvertArrayIfContextHasNoType()
    {
        ((Func<object>)(() => ((ContextName)"named-context").Coerce(new[] { new { } }))).Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void ThrowIfAttemptingToConvertStringIfContextHasNoType()
    {
        ((Func<object>)(() => ((ContextName)"named-context").Coerce("strings"))).Should().Throw<ArgumentException>();
    }
}