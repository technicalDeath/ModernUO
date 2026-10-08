using Server;
using Server.Collections;
using Server.ContextMenus;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PlayerMobileShardHookTests
{
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
}
