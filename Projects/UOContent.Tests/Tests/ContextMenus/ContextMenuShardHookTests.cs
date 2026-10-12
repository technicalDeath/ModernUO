using System.Linq;
using Server;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

// The shard hook on the central context menu builder: null is the stock menu, a handler adds entries after the item's own.
[Collection("Sequential UOContent Tests")]
public class ContextMenuShardHookTests
{
    private static Mobile NewViewer()
    {
        var viewer = new Mobile(World.NewMobile);
        viewer.DefaultMobileInit();

        return viewer;
    }

    [Fact]
    public void WithoutAHandlerAnItemMenuIsTheStockOne()
    {
        ContextMenuSystem.ItemEntriesHandler = null;
        var viewer = NewViewer();
        var item = new Bottle();

        try
        {
            Assert.Empty(ContextMenuSystem.CreateContextMenu(viewer, item).Entries);
        }
        finally
        {
            item.Delete();
            viewer.Delete();
        }
    }

    [Fact]
    public void AHandlerIsGivenTheItemAndTheViewerAndItsEntryIsAddedToTheMenu()
    {
        var viewer = NewViewer();
        var item = new Bottle();
        Item seenItem = null;
        Mobile seenViewer = null;

        try
        {
            ContextMenuSystem.ItemEntriesHandler = (Item target, Mobile from, ref PooledRefList<ContextMenuEntry> list) =>
            {
                seenItem = target;
                seenViewer = from;
                list.Add(new ContextMenuEntry(3050002));
            };

            var entries = ContextMenuSystem.CreateContextMenu(viewer, item).Entries;

            Assert.Same(item, seenItem);
            Assert.Same(viewer, seenViewer);
            Assert.Single(entries);
            Assert.Equal(3050002, entries[0].Number);
        }
        finally
        {
            ContextMenuSystem.ItemEntriesHandler = null;
            item.Delete();
            viewer.Delete();
        }
    }

    [Fact]
    public void AHandlerThatAddsNothingLeavesTheMenuAsItWas()
    {
        var viewer = NewViewer();
        var item = new Bottle();

        try
        {
            ContextMenuSystem.ItemEntriesHandler = static (Item _, Mobile _, ref PooledRefList<ContextMenuEntry> _) => { };

            Assert.Empty(ContextMenuSystem.CreateContextMenu(viewer, item).Entries);
        }
        finally
        {
            ContextMenuSystem.ItemEntriesHandler = null;
            item.Delete();
            viewer.Delete();
        }
    }

    [Fact]
    public void AMobileMenuNeverAsksTheItemHandler()
    {
        var viewer = NewViewer();
        var other = NewViewer();
        var asked = false;

        try
        {
            ContextMenuSystem.ItemEntriesHandler = (Item _, Mobile _, ref PooledRefList<ContextMenuEntry> _) => asked = true;

            ContextMenuSystem.CreateContextMenu(viewer, other);

            Assert.False(asked);
        }
        finally
        {
            ContextMenuSystem.ItemEntriesHandler = null;
            other.Delete();
            viewer.Delete();
        }
    }
}
