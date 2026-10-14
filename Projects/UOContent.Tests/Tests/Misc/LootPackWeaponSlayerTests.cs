using System;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class LootPackWeaponSlayerTests
{
    [Fact]
    public void WithNoFilterEveryMonsterSlayerIsAllowed() => Assert.Null(LootPackEntry.WeaponLootSlayerAllowed);

    [Fact]
    public void TheFilterIsAHookTheShardCanSetAndClear()
    {
        var previous = LootPackEntry.WeaponLootSlayerAllowed;

        try
        {
            LootPackEntry.WeaponLootSlayerAllowed = slayer => slayer == SlayerName.Silver;

            Assert.True(LootPackEntry.WeaponLootSlayerAllowed(SlayerName.Silver));
            Assert.False(LootPackEntry.WeaponLootSlayerAllowed(SlayerName.OrcSlaying));
        }
        finally
        {
            LootPackEntry.WeaponLootSlayerAllowed = previous;
        }
    }
}
