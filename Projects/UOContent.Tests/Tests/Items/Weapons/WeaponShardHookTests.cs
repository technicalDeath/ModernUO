using System;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

// The shard hooks in the pre-AOS weapon formulas (WeaponEvents): null is stock, a delegate changes only the value it is handed.
// In this test process no skill-check handler is registered, so Mobile.CheckSkill always returns false: the stock roll of CheckHit
// is a deterministic miss and only the second roll can produce a hit.
[Collection("Sequential UOContent Tests")]
public class WeaponShardHookTests
{
    [Fact]
    public void WithoutHooksTheSwingDelayUsesTheCurrentStamina()
    {
        WithUor(() =>
        {
            var bow = new Bow();
            var archer = NewMobile(dex: 33);

            Assert.Equal(15000.0 / ((33 + 100) * 20), bow.GetDelay(archer).TotalSeconds, 6);

            bow.Delete();
            archer.Delete();
        });
    }

    [Fact]
    public void AStaminaHookChangesOnlyTheStaminaTheSwingDelayUses()
    {
        WithUor(() =>
        {
            var bow = new Bow();
            var archer = NewMobile(dex: 33);

            try
            {
                var seen = -1;
                WeaponEvents.StaminaForSwingDelay = (_, stamina) =>
                {
                    seen = stamina;
                    return Math.Max(stamina, 60);
                };

                Assert.Equal(15000.0 / ((60 + 100) * 20), bow.GetDelay(archer).TotalSeconds, 6);
                Assert.Equal(33, seen);
                Assert.Equal(33, archer.Stam);

                // Above the floor the hook is a no-op.
                var rested = NewMobile(dex: 100);
                Assert.Equal(15000.0 / ((100 + 100) * 20), bow.GetDelay(rested).TotalSeconds, 6);
                rested.Delete();
            }
            finally
            {
                WeaponEvents.StaminaForSwingDelay = null;
            }

            bow.Delete();
            archer.Delete();
        });
    }

    [Fact]
    public void ATacticsHookChangesOnlyTheValueTheDamageModifierUses()
    {
        WithUor(() =>
        {
            var bow = new Bow();
            var archer = NewMobile(dex: 33);
            archer.Skills.Tactics.Base = 0.0;

            // Tactics 0 is -50% damage, then +20% for 100 strength.
            Assert.Equal(60.0, bow.ScaleDamageOld(archer, 100, false));

            try
            {
                var seen = -1.0;
                WeaponEvents.TacticsForDamage = (_, tactics) =>
                {
                    seen = tactics;
                    return Math.Max(tactics, 30.0);
                };

                Assert.Equal(96.0, bow.ScaleDamageOld(archer, 100, false));
                Assert.Equal(0.0, seen);
                Assert.Equal(0.0, archer.Skills.Tactics.Base);

                archer.Skills.Tactics.Base = 80.0;
                Assert.Equal(156.0, bow.ScaleDamageOld(archer, 100, false));
            }
            finally
            {
                WeaponEvents.TacticsForDamage = null;
            }

            bow.Delete();
            archer.Delete();
        });
    }

    [Fact]
    public void AHitChanceHookIsAskedOnlyAfterAMissAndWithTheStockChanceAndTheAttackSkill()
    {
        WithUor(() =>
        {
            var (bow, attacker, defender) = Duel(attackerArchery: 60.0, defenderSwords: 40.0);

            try
            {
                var calls = 0;
                var seenSkill = -1.0;
                var seenChance = -1.0;
                WeaponEvents.HitChance = (_, skill, chance) =>
                {
                    calls++;
                    seenSkill = skill;
                    seenChance = chance;
                    return chance;
                };

                var hit = bow.CheckHit(attacker, defender);

                Assert.False(hit);
                Assert.Equal(1, calls);
                Assert.Equal(60.0, seenSkill);
                Assert.Equal((60.0 + 50.0) / ((40.0 + 50.0) * 2.0), seenChance, 6);
            }
            finally
            {
                WeaponEvents.HitChance = null;
            }

            bow.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    [Fact]
    public void AHitChanceHookThatReturnsOneTurnsEveryMissIntoAHit()
    {
        WithUor(() =>
        {
            var (bow, attacker, defender) = Duel(attackerArchery: 0.0, defenderSwords: 100.0);

            try
            {
                WeaponEvents.HitChance = static (_, _, _) => 1.0;

                for (var i = 0; i < 300; i++)
                {
                    Assert.True(bow.CheckHit(attacker, defender));
                }
            }
            finally
            {
                WeaponEvents.HitChance = null;
            }

            bow.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    [Fact]
    public void AHitChanceHookThatReturnsTheStockChanceOrNothingLeavesTheOddsAlone()
    {
        WithUor(() =>
        {
            var (bow, attacker, defender) = Duel(attackerArchery: 0.0, defenderSwords: 100.0);

            try
            {
                for (var mode = 0; mode < 3; mode++)
                {
                    WeaponEvents.HitChance = mode switch
                    {
                        0 => null,
                        1 => static (_, _, chance) => chance,
                        _ => static (_, _, chance) => chance - 0.1 // a hook may not lower the chance
                    };

                    for (var i = 0; i < 400; i++)
                    {
                        Assert.False(bow.CheckHit(attacker, defender));
                    }
                }
            }
            finally
            {
                WeaponEvents.HitChance = null;
            }

            bow.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    [Fact]
    public void ALiftedChanceConvertsMissesWithTheRightProbability()
    {
        WithUor(() =>
        {
            // Stock chance (0 + 50) / ((100 + 50) x 2) = 1/6; the hook asks for 0.5, so a miss converts with (0.5 - 1/6) / (1 - 1/6) = 0.4.
            var (bow, attacker, defender) = Duel(attackerArchery: 0.0, defenderSwords: 100.0);

            try
            {
                WeaponEvents.HitChance = static (_, _, _) => 0.5;

                var hits = 0;
                for (var i = 0; i < 6000; i++)
                {
                    hits += bow.CheckHit(attacker, defender) ? 1 : 0;
                }

                // 2400 expected, sigma about 38.
                Assert.InRange(hits, 2400 - 190, 2400 + 190);
            }
            finally
            {
                WeaponEvents.HitChance = null;
            }

            bow.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    [Fact]
    public void AStockChanceOfOneOrMoreNeverReachesTheSecondRoll()
    {
        WithUor(() =>
        {
            // (60 + 50) / ((0 + 50) x 2) is above 1: no miss to convert and nothing for a hook to add.
            var (bow, attacker, defender) = Duel(attackerArchery: 60.0, defenderSwords: 0.0);

            try
            {
                WeaponEvents.HitChance = static (_, _, _) => 1.0;

                Assert.False(bow.CheckHit(attacker, defender));
            }
            finally
            {
                WeaponEvents.HitChance = null;
            }

            bow.Delete();
            attacker.Delete();
            defender.Delete();
        });
    }

    private static (Bow Bow, Mobile Attacker, Mobile Defender) Duel(double attackerArchery, double defenderSwords)
    {
        var bow = new Bow();
        var attacker = NewMobile(dex: 100);
        attacker.AddItem(bow);
        attacker.Skills.Archery.Base = attackerArchery;

        var defender = NewMobile(dex: 100);
        var sword = new Katana();
        defender.AddItem(sword);
        defender.Skills.Swords.Base = defenderSwords;

        Assert.Same(bow, attacker.Weapon);
        Assert.Same(sword, defender.Weapon);
        return (bow, attacker, defender);
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

    private static Mobile NewMobile(int dex)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.Str = 100;
        mobile.Int = 100;
        mobile.Dex = dex;
        mobile.Stam = mobile.StamMax;
        return mobile;
    }
}
