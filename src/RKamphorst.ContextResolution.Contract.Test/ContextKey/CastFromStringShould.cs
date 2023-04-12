using FluentAssertions;
using Newtonsoft.Json.Linq;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextKey;

using ContextKey = Contract.ContextKey;


public class CastFromStringShould
{
    [Fact]
    public void CreateKeyFromString()
    {
        var result = (ContextKey)"{\"context-name\":{ \"property\": \"value\" }}";
        
        result.Id.Should().BeEquivalentTo(new JObject { ["property"] = "value" });
        result.Name.Aliases.Should().BeEquivalentTo(new[] { "context-name" });
        result.Key.Should().Be("{\"context-name\":{\"property\":\"value\"}}");
    }
    
    [Fact]
    public void CreateTypedKeyFromString()
    {
        var result = (ContextKey)"{\"alias-1\":{ \"property\": \"value\" }}";
        
        result.Id.Should().BeEquivalentTo(new StubContextWithAliases { Property = "value" });
        result.Name.Aliases.Should().BeEquivalentTo(new[] { "StubContextWithAliases", "alias-1", "alias-2" });
        result.Key.Should().Be("{\"StubContextWithAliases|alias-1|alias-2\":{\"property\":\"value\"}}");
    }
    
    [Theory]
    [InlineData("malformed context key")]
    [InlineData("{}")]
    public void ThrowArgumentExceptionForBadKey(string malformedKey)
    {
        ((Func<ContextKey>)(() => (ContextKey)malformedKey)).Should().Throw<ArgumentException>();
    }
}