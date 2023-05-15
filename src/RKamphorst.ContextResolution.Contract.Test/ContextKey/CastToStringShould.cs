using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextKey;

using ContextKey = Contract.ContextKey;

public class CastToStringShould
{
    [Theory]
    [MemberData(nameof(TestNamesAndStrings))]
    public void ResultInStrings(ContextKey contextKey, string expectString)
    {
        var result = (string)contextKey;
        result.Should().Be(expectString);
    }

    public static IEnumerable<object[]> TestNamesAndStrings => new[]
    {
        new object[]
        {
            ContextKey.FromTypedContext(new StubContext()), 
            "{\"stubcontext\":{}}"
        },
        new object[]
        {
            ContextKey.FromTypedContext(new StubContextWithAliases{AProperty = "x"}), 
            "{\"stubcontextwithaliases|alias-1|alias-2\":{\"aProperty\":\"x\"}}"
        },
        new object[]
        {
            ContextKey.FromNamedContext("some-weird-name-nobody-knows"), 
            "{\"some-weird-name-nobody-knows\":{}}"
        },
        new object[]
        {
            ContextKey.FromNamedContext("z-context,x-context,a-context"),
            "{\"a-context|x-context|z-context\":{}}"
        }
    };
    
    [Theory]
    [MemberData(nameof(TestNames))]
    public void ResultInStringThatCastsBackToIdenticalName(ContextKey contextKey)
    {
        var result = (string)contextKey;
        var parsedResult = (ContextKey)result;
        parsedResult.Should().Be(contextKey);
    }

    public static IEnumerable<object[]> TestNames => new[]
    {
        new object[] { ContextKey.FromTypedContext(new StubContext()) },
        new object[] { ContextKey.FromTypedContext(new StubContextWithAliases()) },
        new object[] { ContextKey.FromNamedContext("some-weird-name-nobody-knows") },
        new object[] { ContextKey.FromNamedContext("z-context,x-context,a-context") },
        new object[] { ContextKey.FromNamedContext("alias-2|alias-3") },
        new object[] { ContextKey.FromNamedContext("alias-1|alias-2") },
        new object[] { ContextKey.FromNamedContext("alias-1|unknown-alias") },
    };


}