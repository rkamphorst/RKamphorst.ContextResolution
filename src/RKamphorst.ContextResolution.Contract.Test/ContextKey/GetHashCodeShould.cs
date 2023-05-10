using FluentAssertions;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextKey;

using ContextKey = Contract.ContextKey;

public class GetHashCodeShould
{
    [Theory]
    [MemberData(nameof(EqualGroups))]
    public void ReturnTrueForEqual(ContextKey[] equalGroup)
    {
        var hashCodes = equalGroup.Select(k => k.GetHashCode()).ToArray();
        for (int i = 1; i < hashCodes.Length; i++)
        {
            hashCodes[i].Should().Be(hashCodes[0]);
        }
    }
    
    [Theory]
    [MemberData(nameof(UnequalGroups))]
    public void ReturnTrueForUnequal(ContextKey[] unequalGroup)
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
                ContextKey.FromTypedContext(new StubContextWithAliases { AProperty = "prop" }),
                ContextKey.FromNamedContext("alias-1|alias-2", new { aproperty = "prop"}),
                ContextKey.FromNamedContext("alias-1|unknown-alias",new { aproperty = "prop"}),
                ContextKey.FromNamedContext("alias-1",new { aproperty = "prop"}),
                ContextKey.FromTypedContext(new StubContextWithAliases { AProperty = "prop" })
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