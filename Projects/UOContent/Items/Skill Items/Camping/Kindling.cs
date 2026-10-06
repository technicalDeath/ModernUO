using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Regions;

namespace Server.Items;

public enum KindlingFeed
{
    /// <summary>The fire is not this handler's to feed; try the next fire, or light a new one.</summary>
    NotHandled,

    /// <summary>The fire was fed and one Kindling is used.</summary>
    Fed,

    /// <summary>The mobile was told why not; nothing is used and no new fire is lit.</summary>
    Refused
}

public delegate KindlingFeed KindlingFeedHandler(Mobile from, Campfire fire);

[SerializationGenerator(0, false)]
public partial class Kindling : Item
{
    [Constructible]
    public Kindling(int amount = 1) : base(0xDE1)
    {
        Stackable = true;
        Amount = amount;
    }

    /// <summary>Offered each Campfire within a tile of a mobile using Kindling, before a new fire is lit.</summary>
    public static KindlingFeedHandler FeedHandler { get; set; }

    /// <summary>Decides whether a mobile lights a fire. Null keeps the stock Camping skill roll.</summary>
    public static Func<Mobile, bool> IgniteCheck { get; set; }

    public override double DefaultWeight => 5.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (!VerifyMove(from))
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        switch (TryFeed(from))
        {
            case KindlingFeed.Fed:
                {
                    UseOne(from);
                    return;
                }
            case KindlingFeed.Refused:
                {
                    return;
                }
        }

        var fireLocation = GetFireLocation(from);

        if (fireLocation == Point3D.Zero)
        {
            from.SendLocalizedMessage(501695); // There is not a spot nearby to place your campfire.
        }
        else if (!Ignites(from))
        {
            from.SendLocalizedMessage(501696); // You fail to ignite the campfire.
        }
        else
        {
            UseOne(from);

            new Campfire(from).MoveToWorld(fireLocation, from.Map);
        }
    }

    private KindlingFeed TryFeed(Mobile from)
    {
        if (FeedHandler == null || from.Map == null)
        {
            return KindlingFeed.NotHandled;
        }

        foreach (var fire in from.Map.GetItemsInRange<Campfire>(from.Location, 1))
        {
            var result = FeedHandler(from, fire);

            if (result != KindlingFeed.NotHandled)
            {
                return result;
            }
        }

        return KindlingFeed.NotHandled;
    }

    private static bool Ignites(Mobile from) => IgniteCheck?.Invoke(from) ?? from.CheckSkill(SkillName.Camping, 0.0, 100.0);

    private void UseOne(Mobile from)
    {
        Consume();

        if (!Deleted && Parent == null)
        {
            from.PlaceInBackpack(this);
        }
    }

    private Point3D GetFireLocation(Mobile from)
    {
        if (from.Region.IsPartOf<DungeonRegion>())
        {
            return Point3D.Zero;
        }

        if (Parent == null)
        {
            return Location;
        }

        var list = new List<Point3D>(4);

        AddOffsetLocation(from, 0, -1, list);
        AddOffsetLocation(from, -1, 0, list);
        AddOffsetLocation(from, 0, 1, list);
        AddOffsetLocation(from, 1, 0, list);

        if (list.Count == 0)
        {
            return Point3D.Zero;
        }

        return list.RandomElement();
    }

    private static void AddOffsetLocation(Mobile from, int offsetX, int offsetY, List<Point3D> list)
    {
        var map = from.Map;

        var x = from.X + offsetX;
        var y = from.Y + offsetY;

        var loc = new Point3D(x, y, from.Z);

        if (map.CanFit(loc, 1) && from.InLOS(loc))
        {
            list.Add(loc);
        }
        else
        {
            loc = new Point3D(x, y, map.GetAverageZ(x, y));

            if (map.CanFit(loc, 1) && from.InLOS(loc))
            {
                list.Add(loc);
            }
        }
    }
}
