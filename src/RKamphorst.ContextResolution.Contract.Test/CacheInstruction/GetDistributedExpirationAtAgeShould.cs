using FluentAssertions;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.CacheInstruction;

using CacheInstruction = Contract.CacheInstruction;

public class GetDistributedExpirationAtAgeShould
{

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(333)]
    [InlineData(666)]
    public void ReturnZeroForTransient(int ageSeconds)
    {
        var expiration =
            CacheInstruction.Transient.GetDistributedExpirationAtAge(TimeSpan.FromSeconds(ageSeconds));

        expiration.Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(333)]
    [InlineData(666)]
    public void ReturnExpirationMinusAge(int ageSeconds)
    {
        var expiration =
            CacheInstruction.FromTimeSpan(TimeSpan.FromSeconds(600))
                .GetDistributedExpirationAtAge(TimeSpan.FromSeconds(ageSeconds));

        expiration.Should().Be(ageSeconds > 600 ? TimeSpan.Zero : TimeSpan.FromSeconds(600 - ageSeconds));
    }

}