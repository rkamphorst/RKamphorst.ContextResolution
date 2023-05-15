using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextKey;

using ContextKey = Contract.ContextKey;
using ContextName = Contract.ContextName;

public class FromNamedContextShould
{
    [Fact]
    public void CreateKey()
    {
        var name = "context-name";
        var id = new { property = "value" };
        var result = ContextKey.FromNamedContext(name, id);

        result.Id.Should().BeEquivalentTo(id);
        result.Name.Should().Be((ContextName)name);
        result.Key.Should().Be("{\"context-name\":{\"property\":\"value\"}}");
    }
    
    [Fact]
    public void CreateKeyForAliasReferringToType()
    {
        var name = "alias-1";
        var id = new { aproperty = "value" };
        var result = ContextKey.FromNamedContext(name, id);

        result.Id.Should().BeEquivalentTo(new StubContextWithAliases { AProperty = "value" });
        result.Name.Should().Be((ContextName)name);
        result.Key.Should().Be("{\"stubcontextwithaliases|alias-1|alias-2\":{\"aProperty\":\"value\"}}");
    }

    [Fact]
    public void ThrowForNamedKeyForTypeWithStringId()
    {
        ((Func<ContextKey>)(() => ContextKey.FromNamedContext("alias-1", "bad"))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThrowForNamedKeyForTypeWithArrayId()
    {
        ((Func<ContextKey>)(() => ContextKey.FromNamedContext("alias-1", new[]{"bad"}))).Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void ThrowForNamedKeyForTypeWithBoolId()
    {
        ((Func<ContextKey>)(() => ContextKey.FromNamedContext("alias-1", true))).Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void ThrowForNamedKeyWithStringId()
    {
        ((Func<ContextKey>)(() => ContextKey.FromNamedContext("unknown-alias", "bad"))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThrowForNamedKeyWithArrayId()
    {
        ((Func<ContextKey>)(() => ContextKey.FromNamedContext("unknown-alias", new[]{"bad"}))).Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void ThrowForNamedKeyWithBoolId()
    {
        ((Func<ContextKey>)(() => ContextKey.FromNamedContext("unknown-alias", true))).Should().Throw<ArgumentException>();
    }

}