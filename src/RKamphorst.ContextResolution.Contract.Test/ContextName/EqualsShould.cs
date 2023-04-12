using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextName;

using ContextName = Contract.ContextName;

public class EqualsShould
{
    [Theory]
    [MemberData(nameof(EqualGroups))]
    public void ReturnTrueForEqual(ContextName[] equalGroup)
    {
        for (var i = 1; i < equalGroup.Length; i++)
        {
            (equalGroup[0] == equalGroup[i]).Should().BeTrue();
            (equalGroup[0] != equalGroup[i]).Should().BeFalse();
        }
    }
    
    [Theory]
    [MemberData(nameof(UnequalGroups))]
    public void ReturnTrueForUnequal(ContextName[] unequalGroup)
    {
        for (var i = 1; i < unequalGroup.Length; i++)
        {
            (unequalGroup[0] == unequalGroup[i]).Should().BeFalse();
            (unequalGroup[0] != unequalGroup[i]).Should().BeTrue();
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