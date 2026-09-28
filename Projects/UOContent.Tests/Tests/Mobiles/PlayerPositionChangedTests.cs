using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PlayerPositionChangedTests
{
    [Fact]
    public void LocationAndMapChangesNotifySubscribers()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        var calls = 0;

        void Observe(PlayerMobile changed)
        {
            Assert.Same(player, changed);
            calls++;
        }

        PlayerMobile.PositionChanged += Observe;
        try
        {
            player.Location = new Point3D(100, 100, 0);
            Assert.Equal(1, calls);

            player.Map = Map.Felucca;
            Assert.True(calls >= 2);
        }
        finally
        {
            PlayerMobile.PositionChanged -= Observe;
            player.Delete();
        }
    }
}
