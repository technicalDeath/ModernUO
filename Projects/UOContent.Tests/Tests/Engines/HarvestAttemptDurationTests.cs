using System;
using Server.Engines.Harvest;
using Xunit;

namespace UOContent.Tests.Engines;

public class HarvestAttemptDurationTests
{
    // Lumberjacking before AOS: a swing every 1.6 s, the result 0.9 s after the last swing's sound starts.
    private static readonly HarvestDefinition Lumber = new()
    {
        EffectDelay = TimeSpan.FromSeconds(1.6),
        EffectSoundDelay = TimeSpan.FromSeconds(0.9)
    };

    [Theory]
    [InlineData(1, 900)]
    [InlineData(2, 2500)]
    [InlineData(3, 4100)]
    public void AnAttemptLastsItsSwingsLessOneThenTheLastSound(int swings, int milliseconds) =>
        Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), HarvestSystem.AttemptDuration(Lumber, swings));

    [Fact]
    public void ADefinitionWithNoSwingIntervalLastsItsSoundDelay()
    {
        var fishing = new HarvestDefinition { EffectDelay = TimeSpan.Zero, EffectSoundDelay = TimeSpan.FromSeconds(8) };

        Assert.Equal(TimeSpan.FromSeconds(8), HarvestSystem.AttemptDuration(fishing, 1));
    }
}
