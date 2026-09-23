using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ClassicCombatIdentityTests
{
    [Fact]
    public void UorLumberjacking_IsAxeOnly_AndCapsAtTwentyPercent()
    {
        WithUor(() =>
        {
            var attacker = NewMobile();
            attacker.Skills.Tactics.Base = 50.0;

            var axe = new Axe();
            var polearm = new Halberd();
            var mace = new WarMace();

            attacker.Skills.Lumberjacking.Base = 0.0;
            var axeWithoutLumberjacking = axe.ScaleDamageOld(attacker, 100, false);
            var polearmWithoutLumberjacking = polearm.ScaleDamageOld(attacker, 100, false);
            var maceWithoutLumberjacking = mace.ScaleDamageOld(attacker, 100, false);

            attacker.Skills.Lumberjacking.Base = 100.0;

            Assert.Equal(20.0, axe.ScaleDamageOld(attacker, 100, false) - axeWithoutLumberjacking);
            Assert.Equal(polearmWithoutLumberjacking, polearm.ScaleDamageOld(attacker, 100, false));
            Assert.Equal(maceWithoutLumberjacking, mace.ScaleDamageOld(attacker, 100, false));

            axe.Delete();
            polearm.Delete();
            mace.Delete();
            attacker.Delete();
        });
    }

    [Fact]
    public void UorBladedAndPiercingWeapons_ConsumePoisonChargesOnHit()
    {
        WithUor(() =>
        {
            Assert.False(Core.AOS);
            PoisonKinds.Configure();

            var attacker = NewMobile();
            var defender = NewMobile();
            var sword = new Katana { Poison = Poison.Regular, PoisonCharges = 1 };
            var spear = new ShortSpear { Poison = Poison.Regular, PoisonCharges = 1 };

            sword.OnHit(attacker, defender);
            spear.OnHit(attacker, defender);

            Assert.Equal(0, sword.PoisonCharges);
            Assert.Equal(0, spear.PoisonCharges);
            Assert.Equal(WeaponType.Slashing, sword.Type);
            Assert.Equal(WeaponType.Piercing, spear.Type);

            sword.Delete();
            spear.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    [Fact]
    public void UorMacesAndStaves_KeepTheirDistinctWeaponTypesAndStaminaHitEffect()
    {
        WithUor(() =>
        {
            var attacker = NewMobile();
            attacker.Str = attacker.Dex = attacker.Int = 1;
            var defender = NewMobile();
            defender.Hits = defender.HitsMax;
            defender.Stam = defender.StamMax;
            var mace = new WarMace();
            var staff = new QuarterStaff();

            var initialStamina = defender.Stam;
            mace.OnHit(attacker, defender);
            var afterMace = defender.Stam;
            staff.OnHit(attacker, defender);
            var afterStaff = defender.Stam;

            Assert.InRange(initialStamina - afterMace, 3, 5);
            Assert.InRange(afterMace - afterStaff, 3, 5);
            Assert.Equal(WeaponType.Bashing, mace.Type);
            Assert.Equal(WeaponType.Staff, staff.Type);

            mace.Delete();
            staff.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    [Fact]
    public void UorShieldParrying_ScalesShieldArmorAndRequiresAShield()
    {
        WithUor(() =>
        {
            var defender = NewPlayer();
            defender.Skills.Parry.Base = 100.0;
            var shield = new WoodenShield { Layer = Layer.TwoHanded };
            var unequippedArmor = shield.ArmorRating;

            Assert.True(defender.EquipItem(shield));
            Assert.Equal(unequippedArmor * 0.5 + 1.0, shield.ArmorRating, 5);
            Assert.IsAssignableFrom<BaseShield>(defender.FindItemOnLayer(Layer.TwoHanded));

            shield.Delete();
            defender.Delete();
        });
    }

    [Fact]
    public void UorRangedWeapons_PreserveClassicProfilesAndConsumeTheirAmmo()
    {
        WithUor(() =>
        {
            var attacker = NewPlayer();
            var defender = NewMobile();
            var bow = new Bow();
            var crossbow = new Crossbow();
            var heavyCrossbow = new HeavyCrossbow();
            var arrows = new Arrow(2);
            var bolts = new Bolt(2);

            attacker.AddToBackpack(arrows);
            attacker.AddToBackpack(bolts);

            Assert.Equal((9, 41, 20, 10), (bow.OldMinDamage, bow.OldMaxDamage, bow.OldSpeed, bow.MaxRange));
            Assert.Equal((8, 43, 18, 8), (crossbow.OldMinDamage, crossbow.OldMaxDamage, crossbow.OldSpeed, crossbow.MaxRange));
            Assert.Equal((11, 56, 10, 8), (heavyCrossbow.OldMinDamage, heavyCrossbow.OldMaxDamage, heavyCrossbow.OldSpeed, heavyCrossbow.MaxRange));
            Assert.All(new BaseWeapon[] { bow, crossbow, heavyCrossbow }, weapon => Assert.Equal(Layer.TwoHanded, weapon.Layer));

            Assert.True(bow.OnFired(attacker, defender));
            Assert.True(crossbow.OnFired(attacker, defender));
            Assert.Equal(1, arrows.Amount);
            Assert.Equal(1, bolts.Amount);

            bow.Delete();
            crossbow.Delete();
            heavyCrossbow.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    private static void WithUor(Action action)
    {
        var previousExpansion = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.UOR;
            action();
        }
        finally
        {
            Core.Expansion = previousExpansion;
        }
    }

    private static Mobile NewMobile()
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.Str = mobile.Dex = mobile.Int = 100;
        return mobile;
    }

    private static PlayerMobile NewPlayer()
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.Player = true;
        mobile.Str = mobile.Dex = mobile.Int = 100;
        mobile.AddItem(new Backpack());
        return mobile;
    }
}
