using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Engines.CharacterCreation;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Misc;

[Collection("Sequential UOContent Tests")]
public class CharacterCreationStartingGoldTests
{
    [Theory]
    [InlineData(500)]
    [InlineData(0)]
    public void StartingGoldResolverCanReduceOrSuppressTheCreationGoldStack(int amount)
    {
        var original = CharacterCreation.StartingGoldAmount;
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        var addBackpack = typeof(CharacterCreation).GetMethod(
            "AddBackpack", BindingFlags.Static | BindingFlags.NonPublic);

        try
        {
            CharacterCreation.StartingGoldAmount = _ => amount;
            addBackpack.Invoke(null, [player]);

            var gold = player.Backpack.Items.OfType<Gold>().ToArray();
            if (amount == 0)
            {
                Assert.Empty(gold);
            }
            else
            {
                Assert.Equal(amount, Assert.Single(gold).Amount);
            }
            Assert.Contains(player.Backpack.Items, item => item is Dagger);
            Assert.Contains(player.Backpack.Items, item => item is RedBook);
            Assert.Contains(player.Backpack.Items, item => item is Candle);
        }
        finally
        {
            CharacterCreation.StartingGoldAmount = original;
            player.Delete();
        }
    }
}
