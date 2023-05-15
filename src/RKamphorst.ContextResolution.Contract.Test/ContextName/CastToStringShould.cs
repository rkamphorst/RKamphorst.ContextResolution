using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextName;

using ContextName = Contract.ContextName;

public class CastToStringShould
{
    [Theory]
    [MemberData(nameof(TestNamesAndStrings))]
    public void ResultInAliases(ContextName contextName, string expectString)
    {
        var result = (string)contextName;
        result.Should().Be(expectString);
    }

    public static IEnumerable<object[]> TestNamesAndStrings => new[]
    {
        new object[] { (ContextName)typeof(StubContext), "stubcontext" },
        new object[] { (ContextName)typeof(StubContextWithAliases), "stubcontextwithaliases|alias-1|alias-2" },
        new object[] { (ContextName)"some-weird-name-nobody-knows", "some-weird-name-nobody-knows" },
        new object[] { (ContextName)"z-context,x-context,a-context", "a-context|x-context|z-context" },
        new object[] { (ContextName)"alias-1|alias-2", "stubcontextwithaliases|alias-1|alias-2" },
        new object[] { (ContextName)"alias-2|alias-3", "stubcontextwithaliases2|alias-2|alias-3" },
        new object[] { (ContextName)"alias-1|unknown-alias", "stubcontextwithaliases|alias-1|alias-2" }
    };
    
    [Theory]
    [MemberData(nameof(TestNames))]
    public void ResultInStringThatCastsBackToIdenticalName(ContextName contextName)
    {
        var result = (string)contextName;
        var parsedResult = (ContextName)result;
        parsedResult.Should().Be(contextName);
    }

    public static IEnumerable<object[]> TestNames => new[]
    {
        new object[] { (ContextName) typeof(StubContext) },
        new object[] { (ContextName) typeof(StubContextWithAliases)},
        new object[] { (ContextName) "some-weird-name-nobody-knows" },
        new object[] { (ContextName) "z-context,x-context,a-context" },
        new object[] { (ContextName) "alias-2|alias-3" },
        new object[] { (ContextName) "alias-1|alias-2" },
        new object[] { (ContextName) "alias-1|unknown-alias" },
    };

    
}