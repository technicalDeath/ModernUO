using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public sealed class CorpseHookTests
{
    [Fact]
    public void LiftedItemInvokesShardPostTransferObserver()
    {
        var owner = new Mobile((Serial)0x101);
        owner.DefaultMobileInit();
        var looter = new Mobile((Serial)0x102);
        looter.DefaultMobileInit();
        var corpse = new Corpse(owner, owner.Items);
        var item = new Dagger();
        corpse.DropItem(item);

        var calls = 0;
        Corpse.LootResolvedHandler? previous = Corpse.LootResolved;
        Corpse.LootResolved = (from, liftedCorpse, liftedItem) =>
        {
            Assert.Same(looter, from);
            Assert.Same(corpse, liftedCorpse);
            Assert.Same(item, liftedItem);
            calls++;
        };

        try
        {
            corpse.OnItemLifted(looter, item);
        }
        finally
        {
            Corpse.LootResolved = previous;
        }

        Assert.Equal(1, calls);
    }
}
