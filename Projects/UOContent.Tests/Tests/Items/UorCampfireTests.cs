using System;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class UorCampfireTests
{
    private static CampfireTiming Long { get; } = new(
        TimeSpan.FromSeconds(200.0),
        TimeSpan.FromSeconds(300.0),
        TimeSpan.FromSeconds(480.0)
    );

    [Fact]
    public void StockTiming_KeepsTheOriginalSixtyNinetyHundredSecondFire()
    {
        var stock = CampfireTiming.Stock;

        Assert.Equal(TimeSpan.FromSeconds(60.0), stock.Dim);
        Assert.Equal(TimeSpan.FromSeconds(90.0), stock.Out);
        Assert.Equal(TimeSpan.FromSeconds(100.0), stock.Expire);
    }

    [Theory]
    [InlineData(0.0, CampfireStatus.Burning)]
    [InlineData(59.9, CampfireStatus.Burning)]
    [InlineData(60.0, CampfireStatus.Extinguishing)]
    [InlineData(89.9, CampfireStatus.Extinguishing)]
    [InlineData(90.0, CampfireStatus.Off)]
    [InlineData(99.9, CampfireStatus.Off)]
    public void StockTiming_ReportsEachPhaseAtTheOriginalAges(double seconds, CampfireStatus expected) =>
        Assert.Equal(expected, CampfireTiming.Stock.StatusAt(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData(100.0)]
    [InlineData(5000.0)]
    public void StatusAt_IsNullOnceTheFireIsGone(double seconds) =>
        Assert.Null(CampfireTiming.Stock.StatusAt(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void StatusAt_FollowsALongerTiming()
    {
        Assert.Equal(CampfireStatus.Burning, Long.StatusAt(TimeSpan.FromSeconds(199.0)));
        Assert.Equal(CampfireStatus.Extinguishing, Long.StatusAt(TimeSpan.FromSeconds(200.0)));
        Assert.Equal(CampfireStatus.Off, Long.StatusAt(TimeSpan.FromSeconds(300.0)));
        Assert.Equal(CampfireStatus.Off, Long.StatusAt(TimeSpan.FromSeconds(479.0)));
        Assert.Null(Long.StatusAt(TimeSpan.FromSeconds(480.0)));
    }

    [Fact]
    public void Feed_RestartsAnEmberFireAndKeepsTheLongerTiming()
    {
        var fire = new Campfire { Status = CampfireStatus.Off };

        try
        {
            Assert.Equal(CampfireTiming.Stock, fire.Timing);

            fire.Feed(Long);
            Assert.Equal(CampfireStatus.Burning, fire.Status);
            Assert.Equal(Long, fire.Timing);

            // A weaker feeder restarts the burn but cannot shorten what the fire already has.
            fire.Status = CampfireStatus.Extinguishing;
            fire.Feed(CampfireTiming.Stock);
            Assert.Equal(CampfireStatus.Burning, fire.Status);
            Assert.Equal(Long, fire.Timing);
        }
        finally
        {
            fire.Delete();
        }
    }

    [Fact]
    public void ALitFireIsListedAsActiveUntilItIsDeleted()
    {
        var fire = new Campfire();

        Assert.Contains(fire, Campfire.Active);

        fire.Delete();

        Assert.DoesNotContain(fire, Campfire.Active);
    }

    [Fact]
    public void Feed_MovesLitAtButNotCreatedAt()
    {
        var fire = new Campfire();

        try
        {
            var created = fire.CreatedAt;

            Assert.Equal(created, fire.LitAt);

            fire.Feed(Long);

            Assert.Equal(created, fire.CreatedAt);
            Assert.True(fire.LitAt >= created);
        }
        finally
        {
            fire.Delete();
        }
    }

    [Fact]
    public void ANewFireWithoutAProviderOrLighterUsesTheStockTiming()
    {
        var fire = new Campfire();

        try
        {
            Assert.Null(fire.Lighter);
            Assert.Equal(CampfireTiming.Stock, fire.Timing);
            Assert.Equal(CampfireStatus.Burning, fire.Status);
        }
        finally
        {
            fire.Delete();
        }
    }
}
