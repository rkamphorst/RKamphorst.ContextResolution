using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextName;

using ContextName = Contract.ContextName;

public class CastFromStringShould
{
    [Theory]
    [InlineData("StubContext", typeof(StubContext))]
    [InlineData("StubContextWithAliases", typeof(StubContextWithAliases))]
    [InlineData("alias-1", typeof(StubContextWithAliases))]
    [InlineData("StubContextWithAliases,alias-2", typeof(StubContextWithAliases))]
    [InlineData("StubContextWithAliases alias-1 alias-2", typeof(StubContextWithAliases))]
    [InlineData("alias-1|alias-2", typeof(StubContextWithAliases))]
    [InlineData("alias-2|alias-3", typeof(StubContextWithAliases2))]
    [InlineData("named-alias-1|named-alias-2", null)]
    public void MatchCorrectType(string aliasesStr, Type? expectType)
    {
        var result = (ContextName)aliasesStr;
        result.GetContextType().Should().Be(expectType);
    }
    
    
    [Theory]
    [InlineData("StubContext", "StubContext")]
    [InlineData("StubContextWithAliases", "StubContextWithAliases,alias-1,alias-2")]
    [InlineData("alias-1", "StubContextWithAliases,alias-1,alias-2")]
    [InlineData("StubContextWithAliases,alias-2", "StubContextWithAliases,alias-1,alias-2")]
    [InlineData("StubContextWithAliases alias-1 alias-2", "StubContextWithAliases,alias-1,alias-2")]
    [InlineData("alias-1|alias-2", "StubContextWithAliases,alias-1,alias-2")]
    [InlineData("named-alias-1|named-alias-2", "named-alias-1,named-alias-2")]
    public void MatchCorrectAliases(string aliasesStr, string expectAliasesStr)
    {
        var result = (ContextName)aliasesStr;
        
        var expectAliases = expectAliasesStr.Split(",");
        
        result.Aliases.Should().BeEquivalentTo(expectAliases);
    }

    [Fact]
    public void ThrowExceptionIfAliasesAmbiguous()
    {
        // alias-2 matches both StubContextWithAliases and StubContextWithAliases2
        ((Func<ContextName>)(() => (ContextName)"alias-2")).Should()
            .ThrowExactly<ContextNameAmbiguousException>()
            .Where(x =>
                x.ContextTypes.Contains(typeof(StubContextWithAliases)) &&
                x.ContextTypes.Contains(typeof(StubContextWithAliases2)) &&
                x.Aliases.Contains("alias-2"));
    }
    
    [Fact]
    public void ThrowExceptionIfAliasesEmpty()
    {
        // alias-2 matches both StubContextWithAliases and StubContextWithAliases2
        ((Func<ContextName>)(() => (ContextName)"")).Should()
            .Throw<ArgumentException>( );
        
    }

}