using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Multis.Deeds;

[SerializationGenerator(0, false)]
public abstract partial class HouseDeed : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _offset;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _multiID;

    public HouseDeed(int id, Point3D offset) : base(0x14F0)
    {
        LootType = LootType.Newbied;

        _multiID = id;
        _offset = offset;
    }

    public virtual Direction HouseDirection => Direction.South;

    public abstract Rectangle2D[] Area { get; }

    public override double DefaultWeight => 1.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
        {
            from.SendLocalizedMessage(501271); // You already own a house, you may not place another!
        }
        else
        {
            /* House placement cancellation could result in a
             * 60 second delay in the return of your deed.
             */
            from.SendLocalizedMessage(1010433);

            from.Target = new HousePlacementTarget(this);
        }
    }

    public abstract BaseHouse GetHouse(Mobile owner);

    public int NormalPlacementValue => this switch
    {
        StonePlasterHouseDeed or FieldStoneHouseDeed or SmallBrickHouseDeed or
            WoodHouseDeed or WoodPlasterHouseDeed or ThatchedRoofCottageDeed => 43800,
        BrickHouseDeed => 144500,
        TwoStoryWoodPlasterHouseDeed or TwoStoryStonePlasterHouseDeed => 192400,
        TowerDeed => 433200,
        KeepDeed => 665200,
        CastleDeed => 1022800,
        LargePatioDeed => 152800,
        LargeMarbleDeed => 192000,
        SmallTowerDeed => 88500,
        LogCabinDeed => 97800,
        SandstonePatioDeed => 90900,
        VillaDeed => 136500,
        StoneWorkshopDeed => 60600,
        MarbleWorkshopDeed => 63000,
        _ => 0
    };

    public void OnPlacement(Mobile from, Point3D p)
    {
        CompletePlacement(from, p, from.Map, null);
    }

    private void CompletePlacement(Mobile from, Point3D p, Map map, int? confirmedPremium)
    {
        if (Deleted || from.Map != map || !from.CheckAlive())
        {
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
        {
            from.SendLocalizedMessage(501271); // You already own a house, you may not place another!
        }
        else
        {
            var center = new Point3D(p.X - Offset.X, p.Y - Offset.Y, p.Z - Offset.Z);
            var res = HousePlacement.Check(from, MultiID, center, out var toMove, HouseDirection);

            switch (res)
            {
                case HousePlacementResult.Valid:
                    {
                        var premium = 0;
                        if (HousePlacement.PlacementPremium is not null &&
                            !HousePlacement.TryGetPlacementPremium(from, MultiID, center, NormalPlacementValue, out premium))
                        {
                            from.SendMessage("This house's placement price could not be confirmed. Please try again.");
                            return;
                        }

                        if (confirmedPremium is null && premium > 0)
                        {
                            from.SendGump(new WarningGump(
                                $"Rural placement adds a non-refundable premium of {premium:N0} gold. " +
                                $"Your deed covers the normal {NormalPlacementValue:N0} gold house value; " +
                                $"{premium:N0} gold will be withdrawn now. The total cost is " +
                                $"{NormalPlacementValue + premium:N0} gold. The premium is not returned " +
                                "by demolition, re-deeding, or transfer. Continue?",
                                440,
                                260,
                                okay =>
                                {
                                    if (okay)
                                    {
                                        CompletePlacement(from, p, map, premium);
                                    }
                                }
                            ));
                            return;
                        }

                        if (confirmedPremium is not null && confirmedPremium != premium)
                        {
                            from.SendMessage("The rural placement price changed. Please target the land again.");
                            return;
                        }

                        var house = GetHouse(from);
                        if (house is null)
                        {
                            return;
                        }

                        if (from.AccessLevel < AccessLevel.GameMaster && premium > 0 &&
                            !Banker.Withdraw(from, premium))
                        {
                            house.RemoveKeys(from);
                            house.Delete();
                            from.SendLocalizedMessage(1060646);
                            return;
                        }

                        house.MoveToWorld(center, from.Map);
                        Delete();

                        for (var i = 0; i < toMove.Count; ++i)
                        {
                            var o = toMove[i];

                            if (o is Mobile mobile)
                            {
                                mobile.Location = house.BanLocation;
                            }
                            else if (o is Item item)
                            {
                                item.Location = house.BanLocation;
                            }
                        }

                        break;
                    }
                case HousePlacementResult.BadItem:
                case HousePlacementResult.BadLand:
                case HousePlacementResult.BadStatic:
                case HousePlacementResult.BadRegionHidden:
                    {
                        // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
                        from.SendLocalizedMessage(1043287);
                        break;
                    }
                case HousePlacementResult.NoSurface:
                    {
                        from.SendMessage(
                            "The house could not be created here.  Part of the foundation would not be on any surface."
                        );
                        break;
                    }
                case HousePlacementResult.BadRegion:
                    {
                        from.SendLocalizedMessage(501265); // Housing cannot be created in this area.
                        break;
                    }
                case HousePlacementResult.BadRegionTemp:
                    {
                        // Lord British has decreed a 'no build' period, thus you cannot build this house at this time.
                        from.SendLocalizedMessage(501270);
                        break;
                    }
                case HousePlacementResult.BadRegionRaffle:
                    {
                        // You must have a deed for this plot of land in order to build here.
                        from.SendLocalizedMessage(1150493);
                        break;
                    }
            }
        }
    }
}
