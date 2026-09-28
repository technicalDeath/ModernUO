using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class UorNinjitsuItemGateTests
{
    [Fact]
    public void LegacyNinjitsuSkillCannotActivateLaterEraItems()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var smoke = new SmokeBomb();
        var egg = new EggBomb();
        var fukiya = new Fukiya { UsesRemaining = 10 };
        var belt = new LeatherNinjaBelt { UsesRemaining = 10 };
        try
        {
            Core.Expansion = Expansion.UOR;
            player.DefaultMobileInit();
            player.Player = true;
            player.AddItem(new Backpack());
            player.Skills.Ninjitsu.Base = 100;
            player.Skills.Hiding.Base = 100;
            player.Mana = 100;
            var manaBefore = player.Mana;
            player.Backpack.DropItem(smoke);
            player.Backpack.DropItem(egg);
            player.Backpack.DropItem(fukiya);
            player.Backpack.DropItem(belt);

            smoke.OnDoubleClick(player);
            egg.OnDoubleClick(player);
            NinjaWeapon.AttemptShoot(player, fukiya);
            new NinjaWeapon.LoadEntry(6224).OnClick(player, fukiya);
            new NinjaWeapon.UnloadEntry(6225, true).OnClick(player, fukiya);

            Assert.False(smoke.Deleted);
            Assert.False(egg.Deleted);
            Assert.Null(player.Target);
            Assert.Equal(10, fukiya.UsesRemaining);
            Assert.Equal(manaBefore, player.Mana);
            Assert.False(belt.OnEquip(player));
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
            smoke.Delete();
            egg.Delete();
            fukiya.Delete();
            belt.Delete();
        }
    }
}
