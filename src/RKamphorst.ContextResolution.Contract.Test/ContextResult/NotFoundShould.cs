using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextResult;

using ContextResult = Contract.ContextResult;
using CacheInstruction = Contract.CacheInstruction;
using ContextName = Contract.ContextName;

public class NotFoundShould
{
    [Fact]
    public void ReturnNotFoundResult()
    {
        var result = ContextResult.NotFound("alias-1");

        result.IsContextSourceFound.Should().BeFalse();
        result.CacheInstruction.Should().BeEquivalentTo(CacheInstruction.Transient);
        result.Name.Should().Be((ContextName)typeof(StubContextWithAliases));
    }
}