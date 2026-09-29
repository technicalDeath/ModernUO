using System;
using Server;
using Server.Engines.BulkOrders;
using Server.Engines.Craft;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class UorBulkOrderVendorTests
{
    [Fact]
    public void UorKeepsBlacksmithOrdersAndGatesTailoringAndWeaponsmithOrders()
    {
        var previous = Core.Expansion;
        Core.Expansion = Expansion.UOR;
        DefBlacksmithy.Initialize();

        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Skills.Blacksmith.Base = 75.0;
        player.Skills.Tailoring.Base = 75.0;

        var blacksmith = new Blacksmith();
        var weaponsmith = new Weaponsmith();
        var tailor = new Tailor();
        var weaver = new Weaver();
        var tailorDeed = new SmallTailorBOD();
        Item? smithDeed = null;

        try
        {
            Assert.True(blacksmith.SupportsBulkOrders(player));
            Assert.False(weaponsmith.SupportsBulkOrders(player));
            smithDeed = blacksmith.CreateBulkOrder(player, fromContextMenu: true);
            Assert.NotNull(smithDeed);
            Assert.False(tailor.SupportsBulkOrders(player));
            Assert.False(weaver.SupportsBulkOrders(player));
            Assert.Null(tailor.CreateBulkOrder(player, fromContextMenu: true));
            Assert.Null(weaver.CreateBulkOrder(player, fromContextMenu: true));
            Assert.False(tailor.IsValidBulkOrder(tailorDeed));
            Assert.False(weaver.IsValidBulkOrder(tailorDeed));
        }
        finally
        {
            smithDeed?.Delete();
            tailorDeed.Delete();
            blacksmith.Delete();
            weaponsmith.Delete();
            tailor.Delete();
            weaver.Delete();
            player.Delete();
            Core.Expansion = previous;
        }
    }

    [Fact]
    public void AoSRestoresWeaponsmithOrdersAndTailoringOrders()
    {
        var previous = Core.Expansion;
        Core.Expansion = Expansion.AOS;
        DefTailoring.Initialize();

        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Skills.Blacksmith.Base = 75.0;
        player.Skills.Tailoring.Base = 75.0;

        var weaponsmith = new Weaponsmith();
        var tailor = new Tailor();
        var weaver = new Weaver();
        var tailorDeed = new SmallTailorBOD();
        Item? weaponsmithOrder = null;
        Item? tailorOrder = null;
        Item? weaverOrder = null;

        try
        {
            Assert.True(weaponsmith.SupportsBulkOrders(player));
            weaponsmithOrder = weaponsmith.CreateBulkOrder(player, fromContextMenu: true);
            Assert.NotNull(weaponsmithOrder);
            Assert.True(tailor.SupportsBulkOrders(player));
            Assert.True(weaver.SupportsBulkOrders(player));
            tailorOrder = tailor.CreateBulkOrder(player, fromContextMenu: true);
            player.NextTailorBulkOrder = TimeSpan.Zero;
            weaverOrder = weaver.CreateBulkOrder(player, fromContextMenu: true);
            Assert.NotNull(tailorOrder);
            Assert.NotNull(weaverOrder);
            Assert.True(tailor.IsValidBulkOrder(tailorDeed));
            Assert.True(weaver.IsValidBulkOrder(tailorDeed));
        }
        finally
        {
            weaponsmithOrder?.Delete();
            tailorOrder?.Delete();
            weaverOrder?.Delete();
            tailorDeed.Delete();
            weaponsmith.Delete();
            tailor.Delete();
            weaver.Delete();
            player.Delete();
            Core.Expansion = previous;
        }
    }
}
