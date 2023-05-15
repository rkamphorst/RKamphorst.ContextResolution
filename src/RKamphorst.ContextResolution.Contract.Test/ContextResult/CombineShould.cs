using FluentAssertions;
using Newtonsoft.Json.Linq;
using RKamphorst.ContextResolution.Contract.Test.Stubs;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.ContextResult;

using ContextResult = Contract.ContextResult;
using CacheInstruction = Contract.CacheInstruction;
using ContextName = Contract.ContextName;

public class CombineShould
{
    [Fact]
    public void CombineOneTypedResult()
    {
        var result = ContextResult.Combine(
            (ContextName)typeof(StubContextWithAliases),
            new[]
            {
                ContextResult.Success(new StubContextWithAliases { AProperty = "property" }, CacheInstruction.Transient),
            });

        result.GetResult().Should().BeOfType<StubContextWithAliases>();
        result.GetResult().Should().BeEquivalentTo(new StubContextWithAliases
        {
            AProperty = "property",
        });
    }
    
    [Fact]
    public void CombineTwoTypedResults()
    {
        var result = ContextResult.Combine(
            (ContextName)typeof(StubContextWithAliases),
            new[]
            {
                ContextResult.Success(new StubContextWithAliases { AProperty = "property" }, CacheInstruction.Transient),
                ContextResult.Success(new StubContextWithAliases { BProperty = "property2" }, CacheInstruction.Transient)
            });

        result.GetResult().Should().BeOfType<StubContextWithAliases>();
        result.GetResult().Should().BeEquivalentTo(new StubContextWithAliases
        {
            AProperty = "property",
            BProperty = "property2"
        });
    }
    
    [Fact]
    public void CombineTwoUntypedResults()
    {
        var result = ContextResult.Combine(
            (ContextName)"named-context",
            new[]
            {
                ContextResult.Success("named-context", new { property = "property" }, CacheInstruction.Transient),
                ContextResult.Success("named-context", new { property2 = "property2" }, CacheInstruction.Transient)
            });

        result.GetResult().Should().BeEquivalentTo(new JObject
        {
            ["property"] = "property",
            ["property2"] = "property2"
        });
    }
    
    [Fact]
    public void CombineTwoUntypedResultsWhereOneIsNotFound()
    {
        var result = ContextResult.Combine(
            (ContextName)"named-context",
            new[]
            {
                ContextResult.Success("named-context", new JObject { ["property"] = "property" }, CacheInstruction.Transient),
                ContextResult.NotFound("named-context")
            });

        result.GetResult().Should().BeEquivalentTo(new JObject
        {
            ["property"] = "property",
        });
    }
    
    [Fact]
    public void CombineThreeUntypedResultsWhereOneIsNotFound()
    {
        var result = ContextResult.Combine(
            (ContextName)"named-context",
            new[]
            {
                ContextResult.Success("named-context", new { property = "property" }, CacheInstruction.Transient),
                ContextResult.Success("named-context", new { property2 = "property2" }, CacheInstruction.Transient),
                ContextResult.NotFound("named-context")
            });

        result.GetResult().Should().BeEquivalentTo(new JObject
        {
            ["property"] = "property",
            ["property2"] = "property"
        });
    }

    [Fact]
    public void CombineTwoUntypedResultsIntoATypedResult()
    {
        var result = ContextResult.Combine(
            (ContextName)"StubContextWithAliases",
            new[]
            {
                ContextResult.Success("alias-1|alias-2", new { aproperty = "property" }, CacheInstruction.Transient),
                ContextResult.Success("alias-1", new { bproperty = "property2" }, CacheInstruction.Transient)
            });

        result.GetResult().Should().BeOfType<StubContextWithAliases>();
        result.GetResult().Should().BeEquivalentTo(new StubContextWithAliases
        {
            AProperty = "property",
            BProperty = "property2"
        });
    }
    
    [Fact]
    public void CombineTypedAndUntypedResultsIntoATypedResult()
    {
        var result = ContextResult.Combine(
            (ContextName)"StubContextWithAliases",
            new[]
            {
                ContextResult.Success("alias-1|alias-2", new { aproperty = "property" }, CacheInstruction.Transient),
                ContextResult.Success(new StubContextWithAliases { BProperty = "property2" }, CacheInstruction.Transient)
            });

        result.GetResult().Should().BeOfType<StubContextWithAliases>();
        result.GetResult().Should().BeEquivalentTo(new StubContextWithAliases
        {
            AProperty = "property",
            BProperty = "property2"
        });
    }
    
    [Fact]
    public void CombineTypedResultsIntoUntypedResult()
    {
        var result = ContextResult.Combine(
            (ContextName)"named-context",
            new[]
            {
                ContextResult.Success("named-context", new { aproperty = "property" }, CacheInstruction.Transient),
                ContextResult.Success( "named-context", new StubContextWithAliases { BProperty = "property2" }, CacheInstruction.Transient)
            });

        result.GetResult().Should().BeEquivalentTo(new JObject()
        {
            ["AProperty"] = "property",
            ["BProperty"] = "property2"
        });
    }

    [Fact]
    public void ReturnNotFoundIfNoResults()
    {
        var result = ContextResult.Combine(
            (ContextName)typeof(StubContextWithAliases),
            Array.Empty<ContextResult>()
        );

        result.IsContextSourceFound.Should().BeFalse();
    }
    
    [Fact]
    public void ReturnNotFoundIfOnlyNotFoundResults()
    {
        var result = ContextResult.Combine(
            (ContextName)"context-name",
            new[]
            {
                ContextResult.NotFound("context-name"),
                ContextResult.NotFound("context-name"),
                ContextResult.NotFound("context-name")
            }
        );

        result.IsContextSourceFound.Should().BeFalse();
    }

    [Fact]
    public void ThrowIfAContextResultHasDifferentName()
    {
        ((Func<ContextResult>)(() => ContextResult.Combine(
            (ContextName)"StubContextWithAliases",
            new[]
            {
                ContextResult.Success("context-name", new { property = "property" }, CacheInstruction.Transient),
                ContextResult.Success(new StubContextWithAliases { BProperty = "property2" },
                    CacheInstruction.Transient)
            }))).Should().Throw<ArgumentException>();
    }
    
}