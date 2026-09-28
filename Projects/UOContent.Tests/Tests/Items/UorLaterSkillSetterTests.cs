using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class UorLaterSkillSetterTests
{
    private sealed class InspectableSoulStone : SoulStone
    {
        public bool IsUsableBy(Mobile player) => CheckUse(player);
    }

    [Fact]
    public void TranscendenceScrollCannotRaiseASkillUnderUor()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        var scroll = new ScrollofTranscendence(SkillName.Anatomy, 2.0);

        try
        {
            Core.Expansion = Expansion.UOR;
            player.AddToBackpack(scroll);
            player.Skills.Anatomy.Base = 50.0;

            Assert.False(scroll.CanUse(player));
            scroll.Use(player);
            Assert.Equal(50.0, player.Skills.Anatomy.Base);
            Assert.False(scroll.Deleted);
        }
        finally
        {
            Core.Expansion = previous;
            scroll.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void SoulStoneCannotTransferSkillsUnderUor()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        var stone = new InspectableSoulStone();

        try
        {
            Core.Expansion = Expansion.UOR;
            player.AddToBackpack(stone);
            Assert.False(stone.IsUsableBy(player));
        }
        finally
        {
            Core.Expansion = previous;
            stone.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void AlacrityScrollCannotStartLaterEraGainBoostUnderUor()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        var scroll = new ScrollofAlacrity(SkillName.Anatomy);

        try
        {
            Core.Expansion = Expansion.UOR;
            player.AddToBackpack(scroll);

            Assert.False(scroll.CanUse(player));
            scroll.Use(player);
            Assert.Equal(default, player.AcceleratedStart);
            Assert.False(scroll.Deleted);
        }
        finally
        {
            Core.Expansion = previous;
            scroll.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void StatCapScrollCannotBreakUorStatCeiling()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        var scroll = new StatCapScroll(250);

        try
        {
            Core.Expansion = Expansion.UOR;
            player.AddToBackpack(scroll);
            var originalCap = player.StatCap;

            Assert.False(scroll.CanUse(player));
            scroll.Use(player);
            Assert.Equal(originalCap, player.StatCap);
            Assert.False(scroll.Deleted);
        }
        finally
        {
            Core.Expansion = previous;
            scroll.Delete();
            player.Delete();
        }
    }
}
