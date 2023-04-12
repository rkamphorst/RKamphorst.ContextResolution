using FluentAssertions;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextName;
using ContextName = Contract.ContextName;

public class MatchesShould
{
    [Theory]
    [InlineData("a,b,c", "a,b")]
    [InlineData("StubContextWithAliases", "alias-1")]
    [InlineData("StubContextWithAliases", "alias-1,alias-2")]
    public void MatchWhenAllOthersAliasesAreIncluded(string aliases, string otherAliases)
    {
        var result = ((ContextName)aliases).Matches((ContextName)otherAliases);
        result.Should().BeTrue();
    }
    
    [Theory]
    [InlineData("a,b,c", "a,b,d")]
    [InlineData("StubContextWithAliases", "alias-1,alias-3")]
    [InlineData("StubContextWithAliases", "alias-2,alias-3")]
    public void NotMatchWhenNotAllOthersAliasesAreIncluded(string aliases, string otherAliases)
    {
        var result = ((ContextName)aliases).Matches((ContextName)otherAliases);
        result.Should().BeFalse();
    }
}