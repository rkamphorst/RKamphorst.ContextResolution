using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextKey;

using ContextKey = Contract.ContextKey;

public class EqualsShould
{
    [Theory]
    [MemberData(nameof(EqualGroups))]
    public void ReturnTrueForEqual(ContextKey[] equalGroup)
    {
        for (var i = 1; i < equalGroup.Length; i++)
        {
            (equalGroup[0] == equalGroup[i]).Should().BeTrue();
            (equalGroup[0] != equalGroup[i]).Should().BeFalse();
        }
    }
    
    [Theory]
    [MemberData(nameof(UnequalGroups))]
    public void ReturnTrueForUnequal(ContextKey[] unequalGroup)
    {
        for (var i = 1; i < unequalGroup.Length; i++)
        {
            (unequalGroup[0] == unequalGroup[i]).Should().BeFalse();
            (unequalGroup[0] != unequalGroup[i]).Should().BeTrue();
        }
    }

    [Fact]
    public void ReturnFalseForDifferentType()
    {
        var contextKey = ContextKey.FromTypedContext(new StubContextWithAliases { Property = "prop" });

        var result = (contextKey.Equals(new object()));

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnFalseForNull()
    {
        var contextKey = ContextKey.FromTypedContext(new StubContextWithAliases { Property = "prop" });

        var result = (contextKey.Equals(null));

        result.Should().BeFalse();
    }

    public static object[][] EqualGroups => new[]
    {
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithAliases { Property = "prop" }),
                ContextKey.FromNamedContext("alias-1|alias-2", new { property = "prop"}),
                ContextKey.FromNamedContext("alias-1|unknown-alias",new { property = "prop"}),
                ContextKey.FromNamedContext("alias-1",new { property = "prop"}),
                ContextKey.FromTypedContext(new StubContextWithAliases { Property = "prop" })
            }
        },
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithAliases2()),
                ContextKey.FromNamedContext("alias-2|alias-3", new{}),
                ContextKey.FromNamedContext("alias-3|unknown-alias", new{}),
                ContextKey.FromNamedContext("alias-3", new{}),
                ContextKey.FromNamedContext("alias-2|alias-3", new{ lostProperty = "whatever"})
            }
        },
        new object[]
        {
            new[]
            {
                ContextKey.FromNamedContext("alias-A alias-B", new{a="a", b="b"}),
                ContextKey.FromNamedContext("alias-B|alias-A", new{b="b", a="a"})
            }
        }
    };

    public static object[][] UnequalGroups => new[]
    {
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithAliases()),
                ContextKey.FromTypedContext(new StubContextWithAliases2()),
                ContextKey.FromNamedContext("unknown-alias", new { }),
                ContextKey.FromNamedContext("unknown-alias, unknown-alias2", new { })
            }
        },
        new object[]
        {
            new[]
            {
                ContextKey.FromNamedContext("unknown-alias", new { }),
                ContextKey.FromTypedContext(new StubContextWithAliases2()),
                ContextKey.FromTypedContext(new StubContextWithAliases()),
                ContextKey.FromNamedContext("unknown-alias, unknown-alias2", new { })
            }
        }
    };
}