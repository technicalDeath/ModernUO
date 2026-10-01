using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// The two shard-policy hooks on BaseCreature: the TeleportPets follow filter and the
// controlled-placement notification (new master, or a region change while controlled).
[Collection("Sequential UOContent Tests")]
public class PetPlacementHookTests : IDisposable
{
    private readonly List<Mobile> _created = new();
    private readonly Func<BaseCreature, Point3D, Map, bool> _followBefore = BaseCreature.CanFollowMasterHandler;
    private readonly Action<BaseCreature> _placementBefore = BaseCreature.ControlledPlacementChangedHandler;

    private (PlayerMobile master, PetTestStub pet) Spawn(Point3D masterLoc, Point3D petLoc)
    {
        var pair = PetTestSetup.SpawnControlledPet(masterLoc, petLoc);
        _created.Add(pair.master);
        _created.Add(pair.pet);
        return pair;
    }

    public void Dispose()
    {
        BaseCreature.CanFollowMasterHandler = _followBefore;
        BaseCreature.ControlledPlacementChangedHandler = _placementBefore;

        foreach (var m in _created)
        {
            m?.Delete();
        }
    }

    [Fact]
    public void TeleportPets_MovesAFollowerWhenNoHandlerIsSet()
    {
        BaseCreature.CanFollowMasterHandler = null;
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var destination = new Point3D(1100, 1100, 0);

        BaseCreature.TeleportPets(master, destination, Map.Felucca);

        Assert.Equal(destination, pet.Location);
    }

    [Fact]
    public void TeleportPets_SkipsAPetTheHandlerRefusesAndReportsTheDestination()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var destination = new Point3D(1100, 1100, 0);
        BaseCreature seen = null;
        Point3D seenLocation = default;
        BaseCreature.CanFollowMasterHandler = (p, l, _) =>
        {
            seen = p;
            seenLocation = l;
            return false;
        };

        BaseCreature.TeleportPets(master, destination, Map.Felucca);

        Assert.Same(pet, seen);
        Assert.Equal(destination, seenLocation);
        Assert.Equal(new Point3D(1001, 1000, 0), pet.Location);
    }

    [Fact]
    public void NewMasterRaisesThePlacementNotificationButReleaseDoesNot()
    {
        var calls = new List<BaseCreature>();
        BaseCreature.ControlledPlacementChangedHandler = calls.Add;
        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        master.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        var pet = new PetTestStub();
        pet.MoveToWorld(new Point3D(1001, 1000, 0), Map.Felucca);
        _created.Add(master);
        _created.Add(pet);

        pet.SetControlMaster(master);
        Assert.Single(calls);
        Assert.Same(pet, calls[0]);

        pet.SetControlMaster(null);
        Assert.Single(calls);
    }

    [Fact]
    public void ControlledCreatureChangingRegionRaisesThePlacementNotification_WildOneDoesNot()
    {
        var region = new Region("PetHookTestRegion", Map.Felucca, 50, new Rectangle3D(1990, 1990, -128, 20, 20, 256));
        region.Register();
        var calls = new List<BaseCreature>();

        try
        {
            var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
            var wild = new PetTestStub();
            wild.MoveToWorld(new Point3D(1002, 1000, 0), Map.Felucca);
            _created.Add(wild);
            BaseCreature.ControlledPlacementChangedHandler = calls.Add;

            wild.MoveToWorld(new Point3D(2000, 2000, 0), Map.Felucca);
            Assert.Empty(calls);

            pet.MoveToWorld(new Point3D(2000, 2000, 0), Map.Felucca);
            Assert.Contains(pet, calls);
        }
        finally
        {
            region.Unregister();
        }
    }
}
