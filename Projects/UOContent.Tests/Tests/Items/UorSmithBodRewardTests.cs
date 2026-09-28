using Server;
using Server.Engines.BulkOrders;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class UorSmithBodRewardTests
{
    [Fact]
    public void UorSmithBodRewardGroupsExcludeUnapprovedLaterEraItems()
    {
        var previous = Core.Expansion;
        var calculator = new SmithRewardCalculator();

        try
        {
            Core.Expansion = Expansion.UOR;

            foreach (var points in new[] { 200, 400, 500, 550, 600, 625, 650, 675, 700, 800, 900, 950, 1050, 1150, 1200 })
            {
                var group = calculator.LookupRewards(points);

                foreach (var reward in group.Items)
                {
                    var item = reward.Construct();
                    Assert.NotNull(item);
                    Assert.IsNotType<RunicHammer>(item);
                    Assert.IsNotType<PowerScroll>(item);
                    Assert.IsNotType<GargoylesPickaxe>(item);
                    Assert.IsNotType<ProspectorsTool>(item);
                    Assert.IsNotType<PowderOfTemperament>(item);
                    item.Delete();
                }
            }
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Fact]
    public void UorPowerScrollOnlyRewardTierHasNoSpecialItem()
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.UOR;

            Assert.Empty(new SmithRewardCalculator().LookupRewards(800).Items);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Fact]
    public void AossmithBodRewardGroupsRetainRunicHammers()
    {
        var previous = Core.Expansion;
        var calculator = new SmithRewardCalculator();

        try
        {
            Core.Expansion = Expansion.AOS;

            var reward = calculator.LookupRewards(500).Items[0].Construct();

            Assert.IsType<RunicHammer>(reward);
            reward.Delete();
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Fact]
    public void AosSmithBodRewardGroupsRetainPowerScrolls()
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.AOS;

            var reward = new SmithRewardCalculator().LookupRewards(800).Items[0].Construct();

            Assert.IsType<PowerScroll>(reward);
            reward.Delete();
        }
        finally
        {
            Core.Expansion = previous;
        }
    }
}
