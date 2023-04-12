using FluentAssertions;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.CacheInstruction;

using CacheInstruction = Contract.CacheInstruction;

public class ParseShould
{
    [Theory]
    [InlineData("transient", 0, 0)]
    [InlineData("2h ", 7200, 7200)]
    public void ReturnInstruction(string instruction, int localExpirationSeconds, int distributedExpirationSeconds)
    {
        var result = (CacheInstruction)instruction;

        var localExpiration = localExpirationSeconds == Timeout.Infinite
            ? TimeSpan.MaxValue
            : TimeSpan.FromSeconds(localExpirationSeconds);
        var distributedExpiration = distributedExpirationSeconds == Timeout.Infinite
            ? TimeSpan.MaxValue
            : TimeSpan.FromSeconds(distributedExpirationSeconds);

        result.GetLocalExpirationAtAge(TimeSpan.Zero).Should().Be(localExpiration);
        result.GetDistributedExpirationAtAge(TimeSpan.Zero).Should().Be(distributedExpiration);
    }
    

    [Theory]
    [InlineData("unrecognized")]
    [InlineData("trans ient")]
    [InlineData("15 milliseconds")]
    [InlineData("bla")]
    public void ThrowExceptionForBadInstruction(string instruction)
    {
        ((Func<CacheInstruction>)(() => (CacheInstruction) instruction)).Should().Throw<ArgumentException>();
    }
}