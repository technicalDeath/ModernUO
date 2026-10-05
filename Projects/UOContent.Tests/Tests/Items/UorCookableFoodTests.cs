using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class UorCookableFoodTests
{
    [Theory]
    [InlineData(0xDE3)]   // Campfire, first and last frames
    [InlineData(0xDE9)]
    [InlineData(0x461)]   // Sandstone oven/fireplace
    [InlineData(0x48E)]
    [InlineData(0x92B)]   // Stone oven/fireplace
    [InlineData(0x96C)]
    [InlineData(0xFAC)]   // Fire pit
    [InlineData(0x184A)]  // Heating stand
    [InlineData(0x1850)]
    [InlineData(0x398C)]  // Field of Fire
    [InlineData(0x399F)]
    [InlineData(0x197A)]  // Large forge
    [InlineData(0x19A9)]
    [InlineData(0xFB1)]   // Small forge
    public void IsHeatSource_AcceptsEraHeatSources(int itemId) =>
        Assert.True(CookableFood.IsHeatSource(itemId));

    [Theory]
    [InlineData(0xDE2)]   // Just outside the campfire frames
    [InlineData(0xDEA)]
    [InlineData(0xFAD)]
    [InlineData(0xFB0)]   // Anvil, beside the small forge
    [InlineData(0x184D)]  // Gap between the heating stand halves
    [InlineData(0x19AA)]  // Past the large forge
    [InlineData(0)]
    public void IsHeatSource_RejectsOtherTiles(int itemId) =>
        Assert.False(CookableFood.IsHeatSource(itemId));

    [Fact]
    public void IsHeatSource_ReadsTheItemIdOfItems()
    {
        var campfire = new Item(0xDE3);
        var bag = new Item(0xE76);

        try
        {
            Assert.True(CookableFood.IsHeatSource(campfire));
            Assert.False(CookableFood.IsHeatSource(bag));
            Assert.False(CookableFood.IsHeatSource(null));
            Assert.False(CookableFood.IsHeatSource("campfire"));
        }
        finally
        {
            campfire.Delete();
            bag.Delete();
        }
    }

    [Fact]
    public void RawFood_CooksIntoItsCookedCounterpart()
    {
        var fish = new RawFishSteak();
        var lamb = new RawLambLeg();
        var chicken = new RawChickenLeg();
        var ribs = new RawRibs();
        var bird = new RawBird();
        var eggs = new Eggs();

        var cooked = new Item[]
        {
            fish.Cook(), lamb.Cook(), chicken.Cook(), ribs.Cook(), bird.Cook(), eggs.Cook()
        };

        try
        {
            Assert.IsType<FishSteak>(cooked[0]);
            Assert.IsType<LambLeg>(cooked[1]);
            Assert.IsType<ChickenLeg>(cooked[2]);
            Assert.IsType<Ribs>(cooked[3]);
            Assert.IsType<CookedBird>(cooked[4]);
            Assert.IsType<FriedEggs>(cooked[5]);
        }
        finally
        {
            foreach (var item in cooked)
            {
                item.Delete();
            }

            fish.Delete();
            lamb.Delete();
            chicken.Delete();
            ribs.Delete();
            bird.Delete();
            eggs.Delete();
        }
    }
}
