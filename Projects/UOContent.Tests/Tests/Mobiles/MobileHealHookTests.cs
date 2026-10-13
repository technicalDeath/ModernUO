using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class MobileHealHookTests
{
    // The shard runs UOR: the maximum hit points are the stock 50 + Str / 2, with none of the AOS attribute bonuses.
    private static void InUor(Action test)
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.UOR;
            test();
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    private static PlayerMobile NewPlayer(string name)
    {
        var player = new PlayerMobile(World.NewMobile) { Name = name };
        player.DefaultMobileInit();
        player.Player = true;
        player.Str = player.Dex = player.Int = 100;

        return player;
    }

    private static List<(Mobile Patient, Mobile Healer, int Amount)> Watch(Action run)
    {
        var seen = new List<(Mobile, Mobile, int)>();
        void Handler(Mobile patient, Mobile healer, int amount) => seen.Add((patient, healer, amount));

        Mobile.HealApplied += Handler;

        try
        {
            run();
        }
        finally
        {
            Mobile.HealApplied -= Handler;
        }

        return seen;
    }

    [Fact]
    public void AHealReportsWhoWasHealedByWhomAndHowMuch() =>
        InUor(() =>
        {
            var patient = NewPlayer("Patient");
            var healer = NewPlayer("Healer");
            patient.Hits = 40;

            var seen = Watch(() => patient.Heal(25, healer, false));

            Assert.Equal(65, patient.Hits);
            Assert.Single(seen);
            Assert.Same(patient, seen[0].Patient);
            Assert.Same(healer, seen[0].Healer);
            Assert.Equal(25, seen[0].Amount);
        });

    [Fact]
    public void TheAmountIsWhatWasReallyRestoredNotWhatWasAsked() =>
        InUor(() =>
        {
            var patient = NewPlayer("Patient");
            var healer = NewPlayer("Healer");
            patient.Hits = patient.HitsMax - 7;

            var seen = Watch(() => patient.Heal(50, healer, false));

            Assert.Equal(patient.HitsMax, patient.Hits);
            Assert.Equal(7, seen[0].Amount);
        });

    [Fact]
    public void ASelfHealNamesTheMobileAsItsOwnHealer() =>
        InUor(() =>
        {
            var patient = NewPlayer("Patient");
            patient.Hits = 10;

            var seen = Watch(() => patient.Heal(5));

            Assert.Same(patient, seen[0].Healer);
        });

    [Fact]
    public void NothingIsReportedWhenNothingWasRestored() =>
        InUor(() =>
        {
            var patient = NewPlayer("Patient");
            var healer = NewPlayer("Healer");
            patient.Hits = patient.HitsMax;

            var seen = Watch(() => patient.Heal(20, healer, false));

            Assert.Empty(seen);
        });

    [Fact]
    public void NothingIsReportedForADeadMobile() =>
        InUor(() =>
        {
            var patient = NewPlayer("Patient");
            var healer = NewPlayer("Healer");
            patient.Hits = 10;
            patient.Body = 402; // a ghost: a player is not Alive in a ghost body
            Assert.False(patient.Alive);

            var seen = Watch(() => patient.Heal(20, healer, false));

            Assert.Empty(seen);
            Assert.Equal(10, patient.Hits);
        });
}
