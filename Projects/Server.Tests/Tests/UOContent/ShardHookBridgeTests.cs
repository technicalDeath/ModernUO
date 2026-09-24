using Server.Engines.CharacterCreation;
using Server.Mobiles;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class ShardHookBridgeTests
{
    [Fact]
    public void PlayerDeathHandlerCanBeInstalledAndInvoked()
    {
        var previous = PlayerMobile.PlayerDeathHandler;
        var invoked = false;
        var player = new PlayerMobile();

        try
        {
            PlayerMobile.PlayerDeathHandler = value => invoked = ReferenceEquals(value, player);
            PlayerMobile.PlayerDeathHandler(player);
            Assert.True(invoked);
        }
        finally
        {
            PlayerMobile.PlayerDeathHandler = previous;
            player.Delete();
        }
    }

    [Fact]
    public void CharacterCreatedHandlerCanBeInstalledAndInvoked()
    {
        var previous = CharacterCreation.CharacterCreatedHandler;
        var invoked = false;

        try
        {
            CharacterCreation.CharacterCreatedHandler = _ => invoked = true;
            CharacterCreation.CharacterCreatedHandler(null);
            Assert.True(invoked);
        }
        finally
        {
            CharacterCreation.CharacterCreatedHandler = previous;
        }
    }
}
