using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextKey;

using ContextKey = Contract.ContextKey;
using ContextName = Contract.ContextName;

public class FromTypedContextShould
{
    [Fact]
    public void CreateKey()
    {
        var id = new StubContextWithAliases { AProperty = "value" };
        
        var result = ContextKey.FromTypedContext(id);

        result.Id.Should().BeEquivalentTo(id);
        result.Name.Should().Be((ContextName)typeof(StubContextWithAliases));
        result.Key.Should().Be("{\"stubcontextwithaliases|alias-1|alias-2\":{\"aProperty\":\"value\"}}");
    }
}