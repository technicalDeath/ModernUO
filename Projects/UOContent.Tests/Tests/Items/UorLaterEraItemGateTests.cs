using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class UorLaterEraItemGateTests
{
    [Fact]
    public void UorVendorsDoNotStockLaterEraSpellbooks()
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.UOR;

            Assert.Empty(new SBKeeperOfChivalry().BuyInfo);
            Assert.Empty(new SBSamurai().BuyInfo);
            Assert.Empty(new SBNinja().BuyInfo);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Fact]
    public void AosStocksBookOfChivalry()
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.AOS;

            Assert.Equal(typeof(BookOfChivalry), new SBKeeperOfChivalry().BuyInfo[0].Type);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Fact]
    public void SeStocksBushidoAndNinjitsuBooks()
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.SE;

            Assert.Equal(typeof(BookOfBushido), new SBSamurai().BuyInfo[0].Type);
            Assert.Equal(typeof(BookOfNinjitsu), new SBNinja().BuyInfo[0].Type);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Fact]
    public void UorRoninAndEliteNinjaDropNoLaterEraBook()
    {
        var previous = Core.Expansion;
        var ronin = new Ronin();
        var ninja = new EliteNinja();
        var roninCorpse = new Corpse(ronin, new List<Item>());
        var ninjaCorpse = new Corpse(ninja, new List<Item>());

        try
        {
            Core.Expansion = Expansion.UOR;

            ronin.OnDeath(roninCorpse);
            ninja.OnDeath(ninjaCorpse);

            Assert.DoesNotContain(roninCorpse.Items, item => item is BookOfBushido);
            Assert.DoesNotContain(ninjaCorpse.Items, item => item is BookOfNinjitsu);
        }
        finally
        {
            Core.Expansion = previous;
            roninCorpse.Delete();
            ninjaCorpse.Delete();
            ronin.Delete();
            ninja.Delete();
        }
    }

    [Fact]
    public void SeRoninAndEliteNinjaDropTheirBook()
    {
        var previous = Core.Expansion;
        var ronin = new Ronin();
        var ninja = new EliteNinja();
        var roninCorpse = new Corpse(ronin, new List<Item>());
        var ninjaCorpse = new Corpse(ninja, new List<Item>());

        try
        {
            Core.Expansion = Expansion.SE;

            ronin.OnDeath(roninCorpse);
            ninja.OnDeath(ninjaCorpse);

            Assert.Contains(roninCorpse.Items, item => item is BookOfBushido);
            Assert.Contains(ninjaCorpse.Items, item => item is BookOfNinjitsu);
        }
        finally
        {
            Core.Expansion = previous;
            roninCorpse.Delete();
            ninjaCorpse.Delete();
            ronin.Delete();
            ninja.Delete();
        }
    }

    [Fact]
    public void UorCannotEquipThrowingWeapon()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var boomerang = new Boomerang();

        try
        {
            Core.Expansion = Expansion.UOR;
            player.DefaultMobileInit();

            Assert.False(boomerang.CanEquip(player));
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
            boomerang.Delete();
        }
    }

    [Fact]
    public void SaCanEquipThrowingWeapon()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var boomerang = new Boomerang();

        try
        {
            Core.Expansion = Expansion.SA;
            player.DefaultMobileInit();

            Assert.True(boomerang.CanEquip(player));
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
            boomerang.Delete();
        }
    }
}
