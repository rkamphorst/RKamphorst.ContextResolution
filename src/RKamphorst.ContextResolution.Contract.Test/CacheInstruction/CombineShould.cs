using FluentAssertions;
using Xunit;

namespace RKamphorst.ContextResolution.Contract.Test.CacheInstruction;
using CacheInstruction = Contract.CacheInstruction;

public class CombineShould
{
    [Fact]
    public void ReturnTransientForListOfTransients()
    {
        var result = CacheInstruction.Combine(new[]
            { CacheInstruction.Transient, CacheInstruction.Transient, CacheInstruction.Transient, });
        result.Should().Be(CacheInstruction.Transient);
    }
    
   
    [Fact]
    public void ReturnLeastExpirationForCombinationOfTypes()
    {
        var result = CacheInstruction.Combine(new[]
            { CacheInstruction.Transient, (CacheInstruction) "15h" });
        result.GetLocalExpirationAtAge(TimeSpan.Zero).Should().Be(TimeSpan.Zero);
        result.GetDistributedExpirationAtAge(TimeSpan.Zero).Should().Be(TimeSpan.Zero);
    }
    
    [Fact]
    public void ReturnLeastExpiration()
    {
        var result = CacheInstruction.Combine(new[]
            { (CacheInstruction) "18 minutes", (CacheInstruction) "15h", (CacheInstruction) "900 seconds" });
        result.GetLocalExpirationAtAge(TimeSpan.Zero).Should().Be(TimeSpan.FromSeconds(900));
        result.GetDistributedExpirationAtAge(TimeSpan.Zero).Should().Be(TimeSpan.FromSeconds(900));
    }
}