using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextResult;

using ContextResult = Contract.ContextResult;
using CacheInstruction = Contract.CacheInstruction;
using ContextName = Contract.ContextName;

public class SuccessShould
{
    [Fact]
    public void ReturnSuccessResultForTypedResult()
    {
        var result = ContextResult.Success(new StubContextWithAliases { Property = "y" },
            CacheInstruction.Transient);

        result.IsContextSourceFound.Should().BeTrue();
        result.CacheInstruction.Should().BeEquivalentTo(CacheInstruction.Transient);
        result.Name.Should().Be((ContextName)typeof(StubContextWithAliases));
        result.GetResult().Should().BeOfType<StubContextWithAliases>();
        result.GetResult().Should()
            .BeEquivalentTo(new StubContextWithAliases { Property = "y" });
    }
    
    [Fact]
    public void ReturnSuccessResultForNamedResult()
    {
        var result = ContextResult.Success("alias-1", new { property = "y" },
            CacheInstruction.Transient);

        result.IsContextSourceFound.Should().BeTrue();
        result.CacheInstruction.Should().BeEquivalentTo(CacheInstruction.Transient);
        result.Name.Should().Be((ContextName)typeof(StubContextWithAliases));
        result.GetResult().Should().BeOfType<StubContextWithAliases>();
        result.GetResult().Should()
            .BeEquivalentTo(new StubContextWithAliases { Property = "y" });
    }
    
}