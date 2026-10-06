using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class UorTravelConfirmationTests
{
    [Fact]
    public void TheConfirmationHookIsNullByDefaultSoStockTravelIsUnchanged() =>
        Assert.Null(SpellHelper.TravelConfirmation);

    [Fact]
    public void AMoongateAsksTheHookWhereItGoesAndStopsWhilePromptShown()
    {
        var player = new PlayerMobile { Player = true, AccessLevel = AccessLevel.Player };
        var gate = new Moongate(new Point3D(10, 20, 0), Map.Internal);
        Mobile asked = null;
        Map askedMap = null;
        var askedWhere = Point3D.Zero;
        Action proceed = null;

        SpellHelper.TravelConfirmation = (traveler, map, destination, go) =>
        {
            asked = traveler;
            askedMap = map;
            askedWhere = destination;
            proceed = go;
            return true;
        };

        try
        {
            gate.BeginConfirmation(player);

            Assert.Same(player, asked);
            Assert.Same(Map.Internal, askedMap);
            Assert.Equal(new Point3D(10, 20, 0), askedWhere);
            Assert.NotNull(proceed);
            Assert.Equal(Point3D.Zero, player.Location);
        }
        finally
        {
            SpellHelper.TravelConfirmation = null;
            gate.Delete();
            player.Delete();
        }
    }
}
