using FluentAssertions;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.CacheInstruction;

using CacheInstruction = Contract.CacheInstruction;

public class ToStringShould
{
    [Theory]
    [MemberData(nameof(TestInstructions))]
    public void TranslateToParseableString(CacheInstruction instruction)
    {
        var result = (string)instruction;
        var parsedResult = (CacheInstruction)result;

        parsedResult.Should().BeEquivalentTo(instruction);
    }

    public static IEnumerable<object[]> TestInstructions =>
        new[]
        {
            new object[] { CacheInstruction.Transient },
            new object[] { (CacheInstruction)"123 minutes" },
            new object[] { (CacheInstruction)"5 hours" },
            new object[] { (CacheInstruction)"TRANSIENT" },
            new object[] { (CacheInstruction)TimeSpan.FromDays(15) }
        };
}