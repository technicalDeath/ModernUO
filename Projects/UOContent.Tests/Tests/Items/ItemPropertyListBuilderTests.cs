using System;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ItemPropertyListBuilderTests
{
    // A fresh list for the item, built under whatever builder is set now.
    private static int HashOf(Item item)
    {
        item.ClearProperties();
        return item.PropertyList.Hash;
    }

    private static void With(Func<Item, ObjectPropertyList, bool> builder, Action test)
    {
        var previous = Item.PropertyListBuilder;

        try
        {
            Item.PropertyListBuilder = builder;
            test();
        }
        finally
        {
            Item.PropertyListBuilder = previous;
        }
    }

    [Fact]
    public void WithNoBuilderTheStockPropertiesAreUsed()
    {
        var item = new Item(0xF43) { Name = "a hatchet" };

        try
        {
            var stock = HashOf(item);

            With(null, () => Assert.Equal(stock, HashOf(item)));
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void ABuilderThatSaysItHandledTheListReplacesTheStockProperties()
    {
        var item = new Item(0xF43) { Name = "a hatchet" };

        try
        {
            var stock = HashOf(item);

            With(
                (_, list) =>
                {
                    list.Add("only this");
                    return true;
                },
                () => Assert.NotEqual(stock, HashOf(item))
            );

            Assert.Equal(stock, HashOf(item));
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void ABuilderThatDeclinesLeavesTheStockPropertiesAlone()
    {
        var item = new Item(0xF43) { Name = "a hatchet" };

        try
        {
            var stock = HashOf(item);

            With((_, _) => false, () => Assert.Equal(stock, HashOf(item)));
        }
        finally
        {
            item.Delete();
        }
    }

    [Fact]
    public void TheBuilderIsGivenTheItemTheListIsFor()
    {
        var item = new Item(0xF43) { Name = "a hatchet" };
        Item seen = null;

        try
        {
            With(
                (i, _) =>
                {
                    seen = i;
                    return false;
                },
                () => HashOf(item)
            );

            Assert.Same(item, seen);
        }
        finally
        {
            item.Delete();
        }
    }
}
