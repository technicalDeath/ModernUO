using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
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
    private readonly Func<BaseCreature, Mobile, string> _refusalBefore = BaseCreature.AttackCommandRefusalHandler;
    private readonly Func<BaseCreature, Mobile, string> _releaseBefore = BaseCreature.ReleaseCommandRefusalHandler;
    private readonly Action<BaseCreature> _loyaltyBefore = BaseCreature.LoyaltyReleaseHandler;

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
        BaseCreature.AttackCommandRefusalHandler = _refusalBefore;
        BaseCreature.ReleaseCommandRefusalHandler = _releaseBefore;
        BaseCreature.LoyaltyReleaseHandler = _loyaltyBefore;

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

    [Fact]
    public void AttackCommandRefusal_BlocksTheOrderAndNamesThePetAndTarget()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var victim = new PlayerMobile(World.NewMobile);
        victim.DefaultMobileInit();
        victim.MoveToWorld(new Point3D(1002, 1000, 0), Map.Felucca);
        _created.Add(victim);
        pet.ControlOrder = OrderType.Follow;
        BaseCreature seenPet = null;
        Mobile seenTarget = null;
        BaseCreature.AttackCommandRefusalHandler = (p, t) =>
        {
            seenPet = p;
            seenTarget = t;
            return "refused";
        };

        pet.AIObject.EndPickTarget(master, victim, OrderType.Attack);

        Assert.Same(pet, seenPet);
        Assert.Same(victim, seenTarget);
        Assert.Equal(OrderType.Follow, pet.ControlOrder);
        Assert.Null(pet.Combatant);
    }

    [Fact]
    public void AttackCommandRefusal_NullTextLeavesTheOrderAlone()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var target = new PetTestStub();
        target.MoveToWorld(new Point3D(1002, 1000, 0), Map.Felucca);
        _created.Add(target);
        BaseCreature.AttackCommandRefusalHandler = (_, _) => null;

        pet.AIObject.EndPickTarget(master, target, OrderType.Attack);

        Assert.Equal(OrderType.Attack, pet.ControlOrder);
    }

    private static RelayInfo Continue() =>
        new(2, ReadOnlySpan<int>.Empty, ReadOnlySpan<ushort>.Empty, ReadOnlySpan<Range>.Empty, ReadOnlySpan<byte>.Empty);

    [Fact]
    public void ReleaseRefusal_KeepsThePetWhenTheConfirmGumpIsAccepted()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        Mobile seenMaster = null;
        BaseCreature.ReleaseCommandRefusalHandler = (_, from) =>
        {
            seenMaster = from;
            return "no";
        };

        new ConfirmReleaseGump(master, pet).OnResponse(null, Continue());

        Assert.Same(master, seenMaster);
        Assert.True(pet.Controlled);
        Assert.Same(master, pet.ControlMaster);
    }

    [Fact]
    public void ReleaseWithoutARefusal_ReleasesThePetToTheWild()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        BaseCreature.ReleaseCommandRefusalHandler = null;

        new ConfirmReleaseGump(master, pet).OnResponse(null, Continue());

        Assert.False(pet.Controlled);
    }

    [Fact]
    public void LoyaltyRelease_CallsTheHandlerFirstThenReleases()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var wasControlledInHandler = false;
        BaseCreature.LoyaltyReleaseHandler = p => wasControlledInHandler = p.Controlled;

        pet.ReleaseOnLoyaltyLoss();

        Assert.True(wasControlledInHandler);
        Assert.False(pet.Controlled);
    }
}
