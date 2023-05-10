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
        foreach (ContextKey key in equalGroup)
        {
            foreach (ContextKey key2 in equalGroup)
            {
                key.Should().Be(key2);
                (key2 == key).Should().BeTrue();
                (key2 != key).Should().BeFalse();
                (key == key2).Should().BeTrue();
                (key != key2).Should().BeFalse();
            }
        }
    }
    
    [Theory]
    [MemberData(nameof(UnequalGroups))]
    public void ReturnFalseForUnequal(ContextKey[] unequalGroup)
    {
        for (var index = 0; index < unequalGroup.Length; index++)
        {
            ContextKey key = unequalGroup[index];

            for (var index2 = 0; index2 < unequalGroup.Length; index2++)
            {
                if (index == index2) continue;
                
                ContextKey key2 = unequalGroup[index2];
                key.Should().NotBe(key2);
                (key2 == key).Should().BeFalse();
                (key2 != key).Should().BeTrue();
                (key == key2).Should().BeFalse();
                (key != key2).Should().BeTrue();
            }
        }
    }

    [Fact]
    public void ReturnFalseForDifferentType()
    {
        var contextKey = ContextKey.FromTypedContext(new StubContextWithAliases { AProperty = "prop" });

        var result = (contextKey.Equals(new object()));

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnFalseForNull()
    {
        var contextKey = ContextKey.FromTypedContext(new StubContextWithAliases { AProperty = "prop" });

        var result = (contextKey.Equals(null));

        result.Should().BeFalse();
    }

    public static object[][] EqualGroups => new[]
    {
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithAliases { AProperty = "prop", BProperty = "prop" }),
                ContextKey.FromNamedContext("alias-1|alias-2", new { bproperty = "prop", aproperty = "prop"}),
                ContextKey.FromNamedContext("alias-1|unknown-alias",new { aproperty = "prop", bproperty = "prop"}),
                ContextKey.FromNamedContext("alias-1",new { bproperty = "prop", aproperty = "prop"}),
                ContextKey.FromTypedContext(new StubContextWithAliases { BProperty = "prop", AProperty = "prop" })
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
        },
        
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithDictionaryId
                {
                    Id = new Dictionary<string, object>
                    {
                        ["a b c"] = "1 2 3",
                        ["property"] = new Dictionary<string, object>
                        {
                            ["x y z"] = 10,
                            [""] = 11,
                        }
                    }
                }),
                ContextKey.FromTypedContext(new StubContextWithDictionaryId
                {
                    Id = new Dictionary<string, object>
                    {
                        ["property"] = new Dictionary<string, object>
                            {
                                ["x y z"] = 10,
                                [""] = 11,
                        },
                        ["a b c"] = "1 2 3"
                    }
                })

            }
        },
        
        // arrays in IDs are frowned upon.
        // nevertheless, if they are there, they should be treated as sets by the context key logic
        // the next groups are to test this.

        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id = new[] { "1", "2"}}),
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id = new[] { "2", "1"}}),
                ContextKey.FromNamedContext("StubContextWithArrayIds", new { id = new[] { "1", "2"}}),
                ContextKey.FromNamedContext("StubContextWithArrayIds", new { id = new[] { "2", "1"}}),
                ContextKey.FromNamedContext("array-ids", new { id = new[] { "2", "1"}}),
                ContextKey.FromNamedContext("array-ids", new { id = new[] { "1", "2"}})
            }
        },
        
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id2 = new[] { new[] { "1", "2"}, new[] { "3", "4"}} }),
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id2 = new[] { new[] { "2", "1"}, new[] { "4", "3"}} }),
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id2 = new[] { new[] { "3", "4"}, new[] { "1", "2"}} }),
                ContextKey.FromNamedContext("StubContextWithArrayIds", new { Id2 = new[] { new[] { "3", "4"}, new[] { "1", "2"}} }),
                ContextKey.FromNamedContext("StubContextWithArrayIds", new { Id2 = new[] { new[] { "1", "2"}, new[] { "3", "4"}} }),
            }
        },
        
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithArrayIds()
                {
                    Id3 = new []
                    {
                        new StubContextWithArrayIds
                        {
                            Id = new[] { "0" }
                        },
                        new StubContextWithArrayIds
                        {
                            Id2 = new[] { new[] { "1", "2"}, new[] { "3", "4"}}
                        },
                        new StubContextWithArrayIds
                        {
                            Id2 = new[] { new[] { "5", "6"}, new[] { "7", "8", "9"}}
                        },
                        new StubContextWithArrayIds
                        {
                            Id3 = new []
                            {
                                new StubContextWithArrayIds
                                {
                                    Id = new []{"10", "11", "12"}
                                }
                            }
                        }
                    }
                }),
                ContextKey.FromTypedContext(new StubContextWithArrayIds()
                {
                    Id3 = new []
                    {
                        
                        new StubContextWithArrayIds
                        {
                            Id2 = new[] { new[] { "3", "4"}, new[] { "2", "1"}, }
                        },
                        new StubContextWithArrayIds
                        {
                            Id = new[] { "0" }
                        },
                        new StubContextWithArrayIds
                        {
                            Id3 = new []
                            {
                                new StubContextWithArrayIds
                                {
                                    Id = new []{"11", "10", "12"}
                                }
                            }
                        },
                        new StubContextWithArrayIds
                        {
                            Id2 = new[] { new[] { "7", "8", "9"}, new[] { "6", "5" } }
                        }
                    }
                }),
            }
        },
        
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
        },
        
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id = new[] { "1", "2"}}),
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id = new[] { "2", "1", "3"}}),
                ContextKey.FromNamedContext("StubContextWithArrayIds", new { id = new[] { "2"}}),
                ContextKey.FromNamedContext("StubContextWithArrayIds", new { id = new[] { "0", "2", "1"}}),
                ContextKey.FromNamedContext("array-ids", new { id = Array.Empty<string>() }),
                ContextKey.FromNamedContext("array-ids", new { id = new[] { "1" }})
            }
        },
        
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id2 = new[] { new[] { "1", "2"}, new[] { "3" }} }),
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id2 = new[] { new[] { "1"}, new[] { "4", "3"}} }),
                ContextKey.FromTypedContext(new StubContextWithArrayIds() { Id2 = new[] { new[] { "1", "3", "4"}, new[] { "1", "2"}} }),
                ContextKey.FromNamedContext("StubContextWithArrayIds", new { Id2 = new[] { new[] { "3", "4", "5"}, new[] { "1", "2"}} }),
                ContextKey.FromNamedContext("StubContextWithArrayIds", new { Id2 = new[] { new[] { "1", "2"}, Array.Empty<string>() } }),
            }
        },
        
        new object[]
        {
            new[]
            {
                ContextKey.FromTypedContext(new StubContextWithArrayIds()
                {
                    Id3 = new []
                    {
                        new StubContextWithArrayIds
                        {
                            Id = new[] { "0" }
                        },
                        new StubContextWithArrayIds
                        {
                            Id2 = new[] { new[] { "1", "2"}, new[] { "3", "4"}}
                        },
                        new StubContextWithArrayIds
                        {
                            Id2 = new[] { new[] { "5", "6"}, new[] { "7", "8", "9"}}
                        },
                        new StubContextWithArrayIds
                        {
                            Id3 = new []
                            {
                                new StubContextWithArrayIds
                                {
                                    Id = new []{"10", "11", "12"}
                                }
                            }
                        }
                    }
                }),
                ContextKey.FromTypedContext(new StubContextWithArrayIds()
                {
                    Id3 = new []
                    {
                        
                        new StubContextWithArrayIds
                        {
                            Id2 = new[] { new[] { "3", "4"}, new[] { "2", "1"}, }
                        },
                        new StubContextWithArrayIds
                        {
                            Id = new[] { "1" }
                        },
                        new StubContextWithArrayIds
                        {
                            Id3 = new []
                            {
                                new StubContextWithArrayIds
                                {
                                    Id = new []{"11", "10", "12"}
                                }
                            }
                        },
                        new StubContextWithArrayIds
                        {
                            Id2 = new[] { new[] { "7", "8", "9"}, new[] { "6", "5" } }
                        }
                    }
                }),
            }
        },
    };
}