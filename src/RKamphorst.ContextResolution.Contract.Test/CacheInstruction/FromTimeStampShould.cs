using FluentAssertions;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.CacheInstruction;

using CacheInstruction = Contract.CacheInstruction;

public class FromTimeStampShould
{
    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(600)]
    [InlineData(-100)]
    public void ReturnInstruction(int expirationSeconds)
    {
        var result = (CacheInstruction)TimeSpan.FromSeconds(expirationSeconds);

        var expectExpiration = expirationSeconds < 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(expirationSeconds);

        result.GetLocalExpirationAtAge(TimeSpan.Zero).Should().Be(expectExpiration);
        result.GetDistributedExpirationAtAge(TimeSpan.Zero).Should().Be(expectExpiration);
    }
}