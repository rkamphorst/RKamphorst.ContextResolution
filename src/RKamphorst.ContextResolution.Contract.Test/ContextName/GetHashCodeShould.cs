using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextName;

using ContextName = Contract.ContextName;

public class GetHashCodeShould
{
    [Theory]
    [MemberData(nameof(EqualGroups))]
    public void ResultInSameHashCodeForSameName(ContextName[] equalGroup)
    {
        var hashCodes = equalGroup.Select(k => k.GetHashCode()).ToArray();
        for (int i = 1; i < hashCodes.Length; i++)
        {
            hashCodes[i].Should().Be(hashCodes[0]);
        }
    }
    
    [Theory]
    [MemberData(nameof(UnequalGroups))]
    public void ResultInDifferentHashCodeForDifferentName(ContextName[] unequalGroup)
    {
        var hashCodes = unequalGroup.Select(k => k.GetHashCode()).ToArray();
        for (int i = 1; i < hashCodes.Length; i++)
        {
            hashCodes[i].Should().NotBe(hashCodes[0]);
        }
    }

    public static object[][] EqualGroups => new[]
    {
        new object[]
        {
            new[]
            {
                (ContextName)typeof(StubContextWithAliases),
                (ContextName)"alias-1|alias-2",
                (ContextName)"alias-1|unknown-alias",
                (ContextName)"alias-1"
            }
        },
        new object[]
        {
            new[]
            {
                (ContextName)typeof(StubContextWithAliases2),
                (ContextName)"alias-2|alias-3",
                (ContextName)"alias-3|unknown-alias",
                (ContextName)"alias-3"
            }
        },
        new object[]
        {
            new[]
            {
                (ContextName)"alias-A alias-B",
                (ContextName)"alias-B|alias-A"
            }
        }
    };
    
    public static object[][] UnequalGroups => new[]
    {
        new object[]
        {
            new[]
            {
                (ContextName)typeof(StubContextWithAliases),
                (ContextName)typeof(StubContextWithAliases2),
                (ContextName)"unknown-alias",
                (ContextName)"unknown-alias, unknown-alias2"
            }
        },
        new object[]
        {
            new[]
            {
                (ContextName)"unknown-alias",
                (ContextName)typeof(StubContextWithAliases2),
                (ContextName)typeof(StubContextWithAliases),
                (ContextName)"unknown-alias, unknown-alias2"
            }
        }
    };
}