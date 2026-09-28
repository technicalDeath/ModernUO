using Server;
using Server.Engines.BulkOrders;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class StarterBoundBulkOrderTests
{
    private sealed class BoundKatana : Katana
    {
        public override bool Nontransferable => true;
    }

    [Fact]
    public void BoundEquipmentCannotFillSmallBulkOrderButOrdinaryEquipmentCan()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Map = Map.Felucca;
        var deed = new SmallSmithBOD(0, 10, typeof(Katana), 0, 0, false, BulkMaterialType.None);
        var bound = new BoundKatana();
        var ordinary = new Katana();
        try
        {
            deed.EndCombine(player, bound);
            Assert.False(bound.Deleted);
            Assert.Equal(0, deed.AmountCur);

            deed.EndCombine(player, ordinary);
            Assert.True(ordinary.Deleted);
            Assert.Equal(1, deed.AmountCur);
        }
        finally
        {
            ordinary.Delete();
            bound.Delete();
            deed.Delete();
            player.Delete();
        }
    }
}
