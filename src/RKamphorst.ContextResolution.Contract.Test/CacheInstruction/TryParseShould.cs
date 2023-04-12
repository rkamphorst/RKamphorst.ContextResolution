using FluentAssertions;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.CacheInstruction;

using CacheInstruction = Contract.CacheInstruction;

public class TryParseShould
{
    [Fact]
    public void SuccessfullyParseTransientInstruction()
    {
        var success = CacheInstruction.TryParse("transient", out CacheInstruction? result);

        success.Should().BeTrue();
        result.HasValue.Should().BeTrue();
        result!.Value.GetLocalExpirationAtAge(TimeSpan.Zero).Should().Be(TimeSpan.Zero);
        result!.Value.GetDistributedExpirationAtAge(TimeSpan.Zero).Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData("15m", 900)]
    [InlineData("900s", 900)]
    [InlineData("1800 seconds", 1800)]
    [InlineData("2 Hours", 7200)]
    [InlineData("2h ", 7200)]
    [InlineData("1d ", 86400)]
    [InlineData(" 3 DAYS ", 259200)]
    public void SuccessfullyParseExpiration(string instruction, int expectExpirationSeconds)
    {
        var success = CacheInstruction.TryParse(instruction, out CacheInstruction? result);

        success.Should().BeTrue();
        result.HasValue.Should().BeTrue();
        var expectExpiration = TimeSpan.FromSeconds(expectExpirationSeconds);
        result!.Value.GetLocalExpirationAtAge(TimeSpan.Zero).Should().Be(expectExpiration);
        result!.Value.GetDistributedExpirationAtAge(TimeSpan.Zero).Should().Be(expectExpiration);
    }

    [Theory]
    [InlineData("unrecognized")]
    [InlineData("trans ient")]
    [InlineData("15 milliseconds")]
    [InlineData("local transient")]
    public void FailForUnrecognizedInstruction(string instruction)
    {
        var success = CacheInstruction.TryParse(instruction, out CacheInstruction? result);

        success.Should().BeFalse();
    }
}