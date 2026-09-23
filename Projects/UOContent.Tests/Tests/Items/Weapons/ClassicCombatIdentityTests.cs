using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.First;
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

    [Theory]
    [InlineData(1, 1000)]
    [InlineData(25, 1000)]
    [InlineData(63, 750)]
    [InlineData(100, 500)]
    [InlineData(150, 500)]
    public void UorRangedStationaryDelay_ScalesWithDexInPvmAndPvp(int dex, int expectedMilliseconds)
    {
        WithUor(() =>
        {
            foreach (var attacker in new Mobile[] { NewMobile(), NewPlayer() })
            {
                attacker.Dex = dex;

                Assert.Equal(
                    expectedMilliseconds,
                    (int)BaseRanged.GetStationaryDelay(attacker).TotalMilliseconds
                );

                attacker.Delete();
            }
        });
    }

    [Fact]
    public void UorRangedMovementAttempt_DoesNotAdvanceTheSwingAnchor()
    {
        WithUor(() =>
        {
            var attacker = NewPlayer();
            var defender = NewMobile();
            var bow = new Bow();
            attacker.LastSwingTime = 12345;
            attacker.LastMoveTime = Core.TickCount;

            Assert.Equal(TimeSpan.FromMilliseconds(250), bow.OnSwing(attacker, defender));
            Assert.Equal(12345, attacker.LastSwingTime);

            bow.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    [Fact]
    public void UorInstaHit_QuickSwitchUsesLastSwingAndCurrentWeaponDelay()
    {
        WithUor(() =>
        {
            var previousInstaHit = ServerConfiguration.GetSetting("melee.enableInstaHit", false);
            ServerConfiguration.SetSetting("melee.enableInstaHit", true);
            BaseWeapon.Configure();
            Assert.True(ServerConfiguration.GetSetting("melee.enableInstaHit", false));

            var attacker = NewPlayer();
            attacker.Str = 200;
            attacker.Stam = attacker.StamMax;
            var halberd = new Halberd();
            var katana = new Katana();

            Assert.True(halberd.OnEquip(attacker));
            attacker.AddItem(halberd);
            Assert.Equal(0, attacker.NextCombatTime);

            var lastSwing = 12345L;
            attacker.LastSwingTime = lastSwing;
            attacker.NextCombatTime = lastSwing + (long)halberd.GetDelay(attacker).TotalMilliseconds;

            attacker.RemoveItem(halberd);
            Assert.Equal(lastSwing, attacker.LastSwingTime);
            Assert.True(katana.OnEquip(attacker));
            Assert.Equal(lastSwing + (long)katana.GetDelay(attacker).TotalMilliseconds, attacker.NextCombatTime);
            attacker.AddItem(katana);

            Assert.Equal(
                lastSwing + (long)katana.GetDelay(attacker).TotalMilliseconds,
                attacker.NextCombatTime
            );

            // Switching back to a slower weapon cannot bypass its longer delay.
            lastSwing = 23456L;
            attacker.LastSwingTime = lastSwing;
            attacker.NextCombatTime = lastSwing + (long)katana.GetDelay(attacker).TotalMilliseconds;
            attacker.RemoveItem(katana);
            Assert.True(halberd.OnEquip(attacker));
            attacker.AddItem(halberd);
            Assert.Equal(
                lastSwing + (long)halberd.GetDelay(attacker).TotalMilliseconds,
                attacker.NextCombatTime
            );

            attacker.RemoveItem(halberd);
            attacker.LastSwingTime = 0;
            attacker.NextCombatTime = 0;
            Assert.True(halberd.OnEquip(attacker));
            attacker.AddItem(halberd);
            Assert.True(attacker.NextCombatTime <= Core.TickCount);

            halberd.Delete();
            katana.Delete();
            attacker.Delete();

            ServerConfiguration.SetSetting("melee.enableInstaHit", previousInstaHit);
            BaseWeapon.Configure();
        });
    }

    [Fact]
    public void ClassicPrecast_EquipAndUseCancellationMatchesUorStateFlow()
    {
        WithUor(() =>
        {
            var caster = NewMobile();
            var spell = new WeakenSpell(caster);
            var item = new Item();

            caster.Spell = spell;
            spell.State = SpellState.Casting;
            Assert.True(spell.OnCasterUsingObject(item));
            Assert.Equal(SpellState.Casting, spell.State);

            spell.OnCasterEquipping(new Katana());
            Assert.Equal(SpellState.None, spell.State);
            Assert.Null(caster.Spell);

            spell = new WeakenSpell(caster);
            caster.Spell = spell;
            spell.State = SpellState.Sequencing;
            Assert.True(spell.OnCasterEquipping(new Katana()));
            Assert.Equal(SpellState.Sequencing, spell.State);

            Assert.True(spell.OnCasterUsingObject(item));
            Assert.Equal(SpellState.None, spell.State);
            Assert.Null(caster.Spell);

            item.Delete();
            caster.Delete();
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
