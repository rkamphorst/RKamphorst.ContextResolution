using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextName;

using ContextName = Contract.ContextName;

public class CastFromTypeShould
{
    [Theory]
    [InlineData(typeof(StubContext))]
    [InlineData(typeof(StubContextWithAliases))]
    public void RecordType(Type type)
    {
        var result = (ContextName)type;
        result.GetContextType().Should().Be(type);
    }
    
    [Theory]
    [InlineData(typeof(StubContext), "StubContext")]
    [InlineData(typeof(StubContextWithAliases), "StubContextWithAliases,alias-1,alias-2")]
    public void RecordAliases(Type type, string aliasesStr)
    {
        var aliases = aliasesStr.Split(",");
        
        var result = (ContextName)type;

        result.Aliases.Should().BeEquivalentTo(aliases);
    }
    
}