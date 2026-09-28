using Server;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Misc;

[Collection("Sequential UOContent Tests")]
public class CosmeticElfPolicyTests
{
    [Fact]
    public void UorElfUsesHumanEquipmentPermissions()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var shirt = new Shirt();
        var elvenShirt = new ElvenShirt();

        try
        {
            Core.Expansion = Expansion.UOR;
            player.DefaultMobileInit();
            player.MoveToWorld(new Point3D(900, 900, 0), Map.Felucca);
            player.Player = true;
            player.Race = Race.Elf;

            Assert.Same(Race.Human, CosmeticElfPolicy.GameplayRace(player));
            Assert.True(CosmeticElfPolicy.CheckRace(shirt, player));
            Assert.False(CosmeticElfPolicy.CheckRace(elvenShirt, player));
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
            shirt.Delete();
            elvenShirt.Delete();
        }
    }

    [Fact]
    public void MlElfKeepsNativeEquipmentPermissions()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var elvenShirt = new ElvenShirt();

        try
        {
            Core.Expansion = Expansion.ML;
            player.DefaultMobileInit();
            player.MoveToWorld(new Point3D(901, 900, 0), Map.Felucca);
            player.Player = true;
            player.Race = Race.Elf;

            Assert.Same(Race.Elf, CosmeticElfPolicy.GameplayRace(player));
            Assert.True(CosmeticElfPolicy.CheckRace(elvenShirt, player));
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
            elvenShirt.Delete();
        }
    }
}
