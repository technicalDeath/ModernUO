using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Misc;

[Collection("Sequential UOContent Tests")]
public class PlayerCarryWeightHookTests
{
    private static PlayerMobile NewPlayer(int str, int x)
    {
        var player = new PlayerMobile();
        player.DefaultMobileInit();
        player.Player = true;
        player.RawStr = str;
        player.MoveToWorld(new Point3D(x, 920, 0), Map.Felucca);

        if (player.Backpack == null)
        {
            player.AddItem(new Backpack());
        }

        return player;
    }

    [Fact]
    public void WithoutHooksTheLimitsAreStock()
    {
        var previous = Core.Expansion;
        PlayerMobile player = null;
        Bag bag = null;

        try
        {
            Core.Expansion = Expansion.UOR;
            player = NewPlayer(50, 930);
            bag = new Bag();
            player.Backpack.DropItem(bag);

            Assert.Equal(215, player.StockMaxWeight);
            Assert.Equal(215, player.MaxWeight);
            Assert.Equal(400, player.Backpack.MaxWeight);
            Assert.Equal(400, bag.MaxWeight);
        }
        finally
        {
            Core.Expansion = previous;
            bag?.Delete();
            player?.Delete();
        }
    }

    [Fact]
    public void HooksSetTheCarryLimitAndOnlyTheWornBackpack()
    {
        var previous = Core.Expansion;
        PlayerMobile player = null;
        Bag bag = null;
        Backpack loose = null;
        StrongBackpack packAnimal = null;

        try
        {
            Core.Expansion = Expansion.UOR;
            PlayerMobile.MaxWeightAdjust = (pm, stock) => stock + 60;
            Backpack.PlayerMaxWeightAdjust = (_, _) => 550;

            player = NewPlayer(50, 931);
            bag = new Bag();
            player.Backpack.DropItem(bag);
            loose = new Backpack();
            loose.MoveToWorld(new Point3D(932, 920, 0), Map.Felucca);
            packAnimal = new StrongBackpack();

            Assert.Equal(215, player.StockMaxWeight);
            Assert.Equal(275, player.MaxWeight);
            Assert.Equal(550, player.Backpack.MaxWeight);
            Assert.Equal(400, bag.MaxWeight);
            Assert.Equal(400, loose.MaxWeight);
            Assert.Equal(1600, packAnimal.MaxWeight);
        }
        finally
        {
            PlayerMobile.MaxWeightAdjust = null;
            Backpack.PlayerMaxWeightAdjust = null;
            Core.Expansion = previous;
            bag?.Delete();
            loose?.Delete();
            packAnimal?.Delete();
            player?.Delete();
        }
    }
}
