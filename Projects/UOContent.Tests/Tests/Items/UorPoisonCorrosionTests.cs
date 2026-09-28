using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class UorPoisonCorrosionTests
{
    [Fact]
    public void PoisoningSkillReducesCorrosionFrequencyAtHistoricalThresholds()
    {
        Assert.Equal(2, BaseWeapon.GetPoisonCorrosionInterval(4, 50.0));
        Assert.Equal(3, BaseWeapon.GetPoisonCorrosionInterval(4, 50.1));
        Assert.Equal(4, BaseWeapon.GetPoisonCorrosionInterval(4, 99.1));
    }

    [Fact]
    public void OilClothCleansOnlyAfterPoisonedWeaponHasCorroded()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var weapon = new Dagger();
        var cloth = new OilCloth();
        try
        {
            Core.Expansion = Expansion.UOR;
            player.DefaultMobileInit();
            player.Player = true;
            player.AddItem(new Backpack());
            player.AddToBackpack(weapon);
            player.AddToBackpack(cloth);
            weapon.Poison = new PoisonImpl("CorrosionTest", 99, 3, 16, 30, 30.0, 3.0, 5.25, 15, 2);
            weapon.PoisonCharges = 10;
            weapon.MaxHitPoints = 10;
            weapon.HitPoints = 10;

            cloth.OnTarget(player, weapon);
            Assert.Equal(10, weapon.PoisonCharges);
            Assert.False(weapon.IsPoisonCorroded);

            var interval = BaseWeapon.GetPoisonCorrosionInterval(weapon.Poison.Level, 0);
            for (var i = 0; i < interval; i++)
            {
                weapon.ApplyPoisonCorrosionOnHit(player);
            }

            Assert.True(weapon.IsPoisonCorroded);
            Assert.Equal(9, weapon.HitPoints);
            cloth.OnTarget(player, weapon);
            Assert.Null(weapon.Poison);
            Assert.Equal(0, weapon.PoisonCharges);
            Assert.False(weapon.IsPoisonCorroded);
            weapon.ApplyPoisonCorrosionOnHit(player);
            Assert.Equal(9, weapon.HitPoints);
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
            weapon.Delete();
            cloth.Delete();
        }
    }
}
