using Server;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Systems.FeatureFlags;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HousePlacementPolicyHookTests
{
    [Fact]
    public void PlacementRejectsWhenCenterIsDeniedByShardPolicy()
    {
        var previousHook = HousePlacement.PlacementTileAllowed;
        var previousFlag = ContentFeatureFlags.HousePlacement;
        var from = new Mobile();
        from.AccessLevel = AccessLevel.Player;
        from.Map = Map.Felucca;
        var calls = 0;

        try
        {
            ContentFeatureFlags.HousePlacement = true;
            HousePlacement.PlacementTileAllowed = (mobile, map, tile) =>
            {
                Assert.Same(from, mobile);
                Assert.Same(Map.Felucca, map);
                calls++;
                return false;
            };

            var result = HousePlacement.Check(from, 0x64, new Point3D(1000, 1000, 0), out _);
            Assert.Equal(HousePlacementResult.BadRegion, result);
            Assert.True(calls > 0);
        }
        finally
        {
            HousePlacement.PlacementTileAllowed = previousHook;
            ContentFeatureFlags.HousePlacement = previousFlag;
            from.Delete();
        }
    }

    [SkippableFact]
    public void PlacementRejectsWhenAnOccupiedTileIsDeniedByShardPolicy()
    {
        Skip.If(!TestServerInitializer.TileDataLoaded, "Requires client multi and tile data.");

        var previousHook = HousePlacement.PlacementTileAllowed;
        var previousFlag = ContentFeatureFlags.HousePlacement;
        var from = new Mobile();
        from.AccessLevel = AccessLevel.Player;
        from.Map = Map.Felucca;
        var calls = 0;

        try
        {
            ContentFeatureFlags.HousePlacement = true;
            HousePlacement.PlacementTileAllowed = (mobile, map, tile) =>
            {
                Assert.Same(from, mobile);
                Assert.Same(Map.Felucca, map);
                calls++;
                return calls == 1;
            };

            var center = new Point3D(1800, 1500, Map.Felucca.GetAverageZ(1800, 1500));
            var result = HousePlacement.Check(from, 0x64, center, out _);
            Assert.Equal(HousePlacementResult.BadRegion, result);
            Assert.Equal(2, calls);
        }
        finally
        {
            HousePlacement.PlacementTileAllowed = previousHook;
            ContentFeatureFlags.HousePlacement = previousFlag;
            from.Delete();
        }
    }

    [Fact]
    public void PlacementPremiumRequiresAValidBoundedQuote()
    {
        var previousHook = HousePlacement.PlacementPremium;
        var from = new Mobile();
        var center = new Point3D(1800, 1500, 0);
        try
        {
            HousePlacement.PlacementPremium = null;
            Assert.True(HousePlacement.TryGetPlacementPremium(from, 0x64, center, 37000, out var premium));
            Assert.Equal(0, premium);

            HousePlacement.PlacementPremium = (mobile, multiID, point, normalCost) =>
            {
                Assert.Same(from, mobile);
                Assert.Equal(0x64, multiID);
                Assert.Equal(center, point);
                return normalCost;
            };
            Assert.True(HousePlacement.TryGetPlacementPremium(from, 0x64, center, 37000, out premium));
            Assert.Equal(37000, premium);

            HousePlacement.PlacementPremium = (_, _, _, _) => null;
            Assert.False(HousePlacement.TryGetPlacementPremium(from, 0x64, center, 37000, out _));

            HousePlacement.PlacementPremium = (_, _, _, _) => -1;
            Assert.False(HousePlacement.TryGetPlacementPremium(from, 0x64, center, 37000, out _));

            HousePlacement.PlacementPremium = (_, _, _, _) => int.MaxValue;
            Assert.False(HousePlacement.TryGetPlacementPremium(from, 0x64, center, 37000, out _));
        }
        finally
        {
            HousePlacement.PlacementPremium = previousHook;
            from.Delete();
        }
    }

    [Fact]
    public void ClassicDeedPremiumBasisMatchesNormalDeedValue()
    {
        HouseDeed[] deeds =
        [
            new StonePlasterHouseDeed(), new FieldStoneHouseDeed(), new SmallBrickHouseDeed(),
            new WoodHouseDeed(), new WoodPlasterHouseDeed(), new ThatchedRoofCottageDeed(),
            new BrickHouseDeed(), new TwoStoryWoodPlasterHouseDeed(), new TwoStoryStonePlasterHouseDeed(),
            new TowerDeed(), new KeepDeed(), new CastleDeed(), new LargePatioDeed(),
            new LargeMarbleDeed(), new SmallTowerDeed(), new LogCabinDeed(),
            new SandstonePatioDeed(), new VillaDeed(), new StoneWorkshopDeed(), new MarbleWorkshopDeed()
        ];
        var owner = new Mobile();
        try
        {
            foreach (var deed in deeds)
            {
                var house = deed.GetHouse(owner);
                try
                {
                    // The stone workshop deed's vendor price is 60,600 while its house DefaultPrice is 63,000.
                    Assert.Equal(deed is StoneWorkshopDeed ? 60600 : house.DefaultPrice, deed.NormalPlacementValue);
                }
                finally
                {
                    house.RemoveKeys(owner);
                    house.Delete();
                }
            }
        }
        finally
        {
            foreach (var deed in deeds)
            {
                deed.Delete();
            }

            owner.Delete();
        }
    }

}
