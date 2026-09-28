using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class StarterBoundVendorTests
{
    private sealed class BoundKatana : Katana
    {
        public override bool Nontransferable => true;
    }

    private sealed class BoundScissorable : Item, IScissorable
    {
        public bool WasConverted { get; private set; }

        public override bool Nontransferable => true;

        public bool Scissor(Mobile from, Scissors scissors)
        {
            WasConverted = true;
            return true;
        }
    }

    [Fact]
    public void NontransferableStarterItemIsRejectedByScissorAndSalvagePreflight()
    {
        var player = new PlayerMobile { Player = true, AccessLevel = AccessLevel.Player };
        var bound = new BoundScissorable();
        try
        {
            Assert.False(Scissors.CanScissor(player, bound));
            Assert.False(bound.WasConverted);
            Assert.False(bound.Deleted);
        }
        finally
        {
            bound.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void SalvageBagRejectsNontransferableStarterWeaponsButAllowsOrdinaryOutput()
    {
        var bound = new BoundKatana();
        var ordinary = new Katana();
        try
        {
            Assert.False(SalvageBag.CanResmelt(bound));
            Assert.True(SalvageBag.CanResmelt(ordinary));
        }
        finally
        {
            bound.Delete();
            ordinary.Delete();
        }
    }

    [Fact]
    public void SalvageBagEnablesResmeltWhenMetalItemsFollowNonmetalContents()
    {
        var cloth = new Cloth(2);
        var katana = new Katana();
        try
        {
            Assert.True(SalvageBag.HasResmeltableItems([cloth, katana]));
            Assert.False(SalvageBag.HasResmeltableItems([cloth]));
        }
        finally
        {
            cloth.Delete();
            katana.Delete();
        }
    }

    [Fact]
    public void NontransferableStarterItemIsRejectedForNpcSaleAndResaleButOrdinaryOutputRemainsEligible()
    {
        var sellInfo = new GenericSellInfo();
        sellInfo.Add(typeof(BoundKatana), 50);
        sellInfo.Add(typeof(Katana), 50);
        var bound = new BoundKatana();
        var ordinary = new Katana();
        try
        {
            Assert.False(sellInfo.IsSellable(bound));
            Assert.False(sellInfo.IsResellable(bound));
            Assert.True(sellInfo.IsSellable(ordinary));
            Assert.True(sellInfo.IsResellable(ordinary));
        }
        finally
        {
            bound.Delete();
            ordinary.Delete();
        }
    }

    [Fact]
    public void ContainerWithNontransferableStarterContentsIsRejectedForNpcSaleAndResale()
    {
        var sellInfo = new GenericSellInfo();
        sellInfo.Add(typeof(Bag), 50);
        var bag = new Bag();
        var bound = new BoundKatana();
        bag.DropItem(bound);
        try
        {
            Assert.False(sellInfo.IsSellable(bag));
            Assert.False(sellInfo.IsResellable(bag));
        }
        finally
        {
            bag.Delete();
        }
    }

    [Fact]
    public void ContainerWithNontransferableStarterContentsCannotEnterSecureTrade()
    {
        var owner = new PlayerMobile { Player = true, AccessLevel = AccessLevel.Player };
        var recipient = new PlayerMobile { Player = true, AccessLevel = AccessLevel.Player };
        owner.AddItem(new Backpack());
        recipient.AddItem(new Backpack());
        var bag = new Bag();
        bag.DropItem(new BoundKatana());
        owner.Backpack.DropItem(bag);
        try
        {
            Assert.False(owner.CheckTrade(recipient, bag, null, false, true, 0, 0));
        }
        finally
        {
            owner.Delete();
            recipient.Delete();
        }
    }

    [Fact]
    public void ContainerWithNontransferableStarterContentsCannotEnterPlayerVendorStock()
    {
        var owner = new PlayerMobile { Player = true, AccessLevel = AccessLevel.Player };
        owner.AddItem(new Backpack());
        var bag = new Bag();
        bag.DropItem(new BoundKatana());
        owner.Backpack.DropItem(bag);
        var vendor = new PlayerVendor(owner, null!);
        try
        {
            Assert.False(vendor.OnDragDrop(owner, bag));
            Assert.Same(owner.Backpack, bag.Parent);
            Assert.Null(vendor.GetVendorItem(bag));
        }
        finally
        {
            vendor.Delete();
            owner.Delete();
        }
    }

    [Fact]
    public void OrdinaryOutputRemainsEligibleForSecureTradeAndPlayerVendorStock()
    {
        var owner = new PlayerMobile { Player = true, AccessLevel = AccessLevel.Player };
        var recipient = new PlayerMobile { Player = true, AccessLevel = AccessLevel.Player };
        owner.AddItem(new Backpack());
        recipient.AddItem(new Backpack());
        var ordinary = new Katana();
        owner.Backpack.DropItem(ordinary);
        var vendor = new PlayerVendor(owner, null!);
        try
        {
            Assert.True(owner.CheckTrade(recipient, ordinary, null, false, true, 0, 0));
            Assert.True(vendor.OnDragDrop(owner, ordinary));
            Assert.NotNull(vendor.GetVendorItem(ordinary));
        }
        finally
        {
            vendor.Delete();
            owner.Delete();
            recipient.Delete();
        }
    }
}
