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
        for (var i = 0; i < equalGroup.Length; i++)
        {
            for (var j = 0; j < equalGroup.Length; j++)
            {
                equalGroup[i].Should().Be(equalGroup[j]);
                equalGroup[j].Should().Be(equalGroup[i]);
                (equalGroup[j] == equalGroup[i]).Should().BeTrue();
                (equalGroup[j] != equalGroup[i]).Should().BeFalse();
                (equalGroup[i] == equalGroup[j]).Should().BeTrue();
                (equalGroup[i] != equalGroup[j]).Should().BeFalse();
            }
        }
    }
    
    [Theory]
    [MemberData(nameof(UnequalGroups))]
    public void ReturnFalseForUnequal(ContextName[] unEqualGroup)
    {
        for (var i = 0; i < unEqualGroup.Length; i++)
        {
            for (var j = 0; j < unEqualGroup.Length; j++)
            {
                if (i == j) continue;
                
                unEqualGroup[i].Should().NotBe(unEqualGroup[j]);
                unEqualGroup[j].Should().NotBe(unEqualGroup[i]);
                (unEqualGroup[j] == unEqualGroup[i]).Should().BeFalse();
                (unEqualGroup[j] != unEqualGroup[i]).Should().BeTrue();
                (unEqualGroup[i] == unEqualGroup[j]).Should().BeFalse();
                (unEqualGroup[i] != unEqualGroup[j]).Should().BeTrue();
            }
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
                (ContextName)"alias-3",
                (ContextName)"ALIAS-3",
            }
        },
        new object[]
        {
            new[]
            {
                (ContextName)"alias-A alias-B",
                (ContextName)"alias-B|alias-A"
            }
        },
        new object[]
        {
            new[]
            {
                (ContextName)"alias-a ALIAS-B",
                (ContextName)"alias-b Alias-a"
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