using System;
using System.Collections.Generic;
using Server;
using Server.Collections;
using Server.ContextMenus;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PlayerMobileShardHookTests
{
    // The shard runs UOR, where the old guild system is in force (the new one is Core.SE) and a Guild or Alliance line reaches the hook.
    private static void InExpansion(Expansion expansion, Action test)
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = expansion;
            test();
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Fact]
    public void ANameSuffixHandlerAddsItsTagToTheSuffix()
    {
        var player = new PlayerMobile(World.NewMobile) { Name = "Tester" };

        try
        {
            PlayerMobile.NameSuffixHandler = static (_, suffix) => suffix.Length == 0 ? "[Tag]" : $"{suffix} [Tag]";

            Assert.Equal("[Tag]", player.ApplyNameSuffix(""));
            Assert.Equal("the Brave [Tag]", player.ApplyNameSuffix("the Brave"));
        }
        finally
        {
            PlayerMobile.NameSuffixHandler = null;
        }
    }

    [Fact]
    public void WithoutAHandlerTheSuffixIsUnchanged()
    {
        PlayerMobile.NameSuffixHandler = null;
        var player = new PlayerMobile(World.NewMobile) { Name = "Tester" };

        Assert.Equal("the Brave", player.ApplyNameSuffix("the Brave"));
    }

    [Fact]
    public void AnExtraPacketFlagsHandlerAddsItsBitsToTheStockFlags()
    {
        var player = new PlayerMobile(World.NewMobile) { Name = "Tester", Hidden = true };

        try
        {
            var stock = player.GetPacketFlags(true);
            Assert.Equal(0x80, stock & 0x80);
            Assert.Equal(0, stock & 0x20);

            PlayerMobile.ExtraPacketFlagsHandler = static _ => 0x20;

            Assert.Equal(stock | 0x20, player.GetPacketFlags(true));
            Assert.Equal(stock | 0x20, player.GetPacketFlags(false));
        }
        finally
        {
            PlayerMobile.ExtraPacketFlagsHandler = null;
        }

        Assert.Equal(0, player.GetPacketFlags(true) & 0x20);
    }

    [Fact]
    public void AContextMenuHandlerCanAddAnEntryForAnotherMobilesMenu()
    {
        var target = new PlayerMobile(World.NewMobile) { Name = "Target" };
        var from = new PlayerMobile(World.NewMobile) { Name = "Viewer" };

        try
        {
            PlayerMobile.ContextMenuEntriesHandler = static (PlayerMobile _, Mobile _, ref PooledRefList<ContextMenuEntry> list) =>
                list.Add(new ContextMenuEntry(3050001, 1));

            var entries = PooledRefList<ContextMenuEntry>.Create();
            target.GetContextMenuEntries(from, ref entries);

            Assert.Contains(entries.ToArray(), static e => e.Number == 3050001 && e.Range == 1);

            entries.Dispose();
        }
        finally
        {
            PlayerMobile.ContextMenuEntriesHandler = null;
        }
    }

    [Theory]
    [InlineData(MessageType.Guild)]
    [InlineData(MessageType.Alliance)]
    public void AGuildSpeechHandlerIsGivenAGuildOrAllianceLineAndTakesItWhenItReturnsTrue(MessageType type) =>
        InExpansion(
            Expansion.UOR,
            () =>
            {
                var player = new PlayerMobile(World.NewMobile) { Name = "Tester" };
                var calls = new List<(PlayerMobile Speaker, string Text, MessageType Type, int Hue)>();

                try
                {
                    PlayerMobile.GuildSpeechHandler = (speaker, text, kind, hue) =>
                    {
                        calls.Add((speaker, text, kind, hue));

                        return true;
                    };

                    player.DoSpeech("hello guild", [], type, 0x44);

                    Assert.Equal([(player, "hello guild", type, 0x44)], calls);
                }
                finally
                {
                    PlayerMobile.GuildSpeechHandler = null;
                }
            }
        );

    [Fact]
    public void AGuildSpeechHandlerIsNotAskedAboutOrdinarySpeech() =>
        InExpansion(
            Expansion.UOR,
            () =>
            {
                var player = new PlayerMobile(World.NewMobile) { Name = "Tester" };
                var asked = 0;

                try
                {
                    PlayerMobile.GuildSpeechHandler = (_, _, _, _) =>
                    {
                        asked++;

                        return true;
                    };

                    player.DoSpeech("hello", [], MessageType.Regular, 0x3B2);
                    player.DoSpeech("hello", [], MessageType.Whisper, 0x3B2);
                    player.DoSpeech("hello", [], MessageType.Yell, 0x3B2);

                    Assert.Equal(0, asked);
                }
                finally
                {
                    PlayerMobile.GuildSpeechHandler = null;
                }
            }
        );

    [Fact]
    public void AGuildSpeechHandlerThatReturnsFalseLeavesTheStockPathToRun() =>
        InExpansion(
            Expansion.UOR,
            () =>
            {
                var player = new PlayerMobile(World.NewMobile) { Name = "Tester" };
                var asked = 0;

                try
                {
                    PlayerMobile.GuildSpeechHandler = (_, _, _, _) =>
                    {
                        asked++;

                        return false;
                    };

                    player.DoSpeech("hello guild", [], MessageType.Guild, 0x44);

                    Assert.Equal(1, asked);
                }
                finally
                {
                    PlayerMobile.GuildSpeechHandler = null;
                }
            }
        );

    [Fact]
    public void TheNewGuildSystemKeepsItsOwnChatAndNeverAsksTheHandler() =>
        InExpansion(
            Expansion.SE,
            () =>
            {
                var player = new PlayerMobile(World.NewMobile) { Name = "Tester" };
                var asked = 0;

                try
                {
                    PlayerMobile.GuildSpeechHandler = (_, _, _, _) =>
                    {
                        asked++;

                        return true;
                    };

                    player.DoSpeech("hello guild", [], MessageType.Guild, 0x44);

                    Assert.Equal(0, asked);
                }
                finally
                {
                    PlayerMobile.GuildSpeechHandler = null;
                }
            }
        );

    // A poisoned player reads HitsMax, which needs the mobile's equipment and skills initialised.
    private static PlayerMobile NewPoisonTarget()
    {
        var player = new PlayerMobile(World.NewMobile) { Name = "Tester" };
        player.DefaultMobileInit();

        return player;
    }

    private static PoisonImpl TestPoison(int index, int level) => new($"HookTest{index}", index, level, 4, 26, 2.5, 3.5, 3.0, 10, 2);

    [Fact]
    public void WithoutAPoisonHandlerAPoisonLandsAsBefore()
    {
        PlayerMobile.PoisonImmunityHandler = null;
        var player = NewPoisonTarget();

        try
        {
            var result = player.ApplyPoison(null, TestPoison(97, 0));

            Assert.Equal(ApplyPoisonResult.Poisoned, result);
            Assert.True(player.Poisoned);
        }
        finally
        {
            player.Poison = null;
        }
    }

    [Fact]
    public void APoisonHandlerIsGivenThePlayerTheSourceAndThePoisonAndMayRefuseIt()
    {
        var player = NewPoisonTarget();
        var source = new Mobile(World.NewMobile);
        var poison = TestPoison(96, 0);
        PlayerMobile seenPlayer = null;
        Mobile seenSource = null;
        Poison seenPoison = null;

        try
        {
            PlayerMobile.PoisonImmunityHandler = (target, from, applied) =>
            {
                seenPlayer = target;
                seenSource = from;
                seenPoison = applied;

                return true;
            };

            var result = player.ApplyPoison(source, poison);

            Assert.Equal(ApplyPoisonResult.Immune, result);
            Assert.False(player.Poisoned);
            Assert.Same(player, seenPlayer);
            Assert.Same(source, seenSource);
            Assert.Same(poison, seenPoison);
        }
        finally
        {
            PlayerMobile.PoisonImmunityHandler = null;
            player.Poison = null;
        }
    }

    [Fact]
    public void APoisonHandlerThatAcceptsLeavesThePoisonToLand()
    {
        var player = NewPoisonTarget();

        try
        {
            PlayerMobile.PoisonImmunityHandler = static (_, _, _) => false;

            Assert.Equal(ApplyPoisonResult.Poisoned, player.ApplyPoison(null, TestPoison(95, 0)));
            Assert.True(player.Poisoned);
        }
        finally
        {
            PlayerMobile.PoisonImmunityHandler = null;
            player.Poison = null;
        }
    }

    [Fact]
    public void APoisonHandlerIsNotAskedWhenAnEqualOrStrongerPoisonIsAlreadyActive()
    {
        var player = NewPoisonTarget();
        var asked = 0;

        try
        {
            player.ApplyPoison(null, TestPoison(94, 2));

            PlayerMobile.PoisonImmunityHandler = (_, _, _) =>
            {
                asked++;

                return true;
            };

            Assert.Equal(ApplyPoisonResult.HigherPoisonActive, player.ApplyPoison(null, TestPoison(93, 0)));
            Assert.Equal(0, asked);
        }
        finally
        {
            PlayerMobile.PoisonImmunityHandler = null;
            player.Poison = null;
        }
    }
}
