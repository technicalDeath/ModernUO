using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

public enum CampfireStatus
{
    Burning,
    Extinguishing,
    Off
}

/// <summary>When a fire dims, burns down to embers, and is gone, as offsets from when it was lit or last fed.</summary>
public readonly record struct CampfireTiming(TimeSpan Dim, TimeSpan Out, TimeSpan Expire)
{
    public static CampfireTiming Stock { get; } = new(
        TimeSpan.FromSeconds(60.0),
        TimeSpan.FromSeconds(90.0),
        TimeSpan.FromSeconds(100.0)
    );

    /// <summary>The state a fire of this age is in, or null once it is gone.</summary>
    public CampfireStatus? StatusAt(TimeSpan age)
    {
        if (age >= Expire)
        {
            return null;
        }

        if (age >= Out)
        {
            return CampfireStatus.Off;
        }

        return age >= Dim ? CampfireStatus.Extinguishing : CampfireStatus.Burning;
    }
}

[SerializationGenerator(0, false)]
public partial class Campfire : Item
{
    public const int SecureRange = 7;

    private static readonly Dictionary<Mobile, CampfireEntry> _table = [];

    private static readonly HashSet<Campfire> _active = [];

    private readonly List<CampfireEntry> _entries;

    private TimerExecutionToken _timerToken;

    public Campfire() : this(null)
    {
    }

    public Campfire(Mobile lighter) : base(0xDE3)
    {
        Movable = false;
        Light = LightType.Circle300;

        Lighter = lighter;
        Timing = lighter != null && TimingProvider != null ? TimingProvider(lighter) : CampfireTiming.Stock;
        LitAt = Core.Now;
        CreatedAt = LitAt;

        _active.Add(this);
        _entries = [];
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), OnTick, out _timerToken);
    }

    /// <summary>Every fire that currently exists. Fires are never saved, so this needs no rebuild after a restart.</summary>
    public static IReadOnlyCollection<Campfire> Active => _active;

    /// <summary>Chooses the timing of a fire a mobile lights. Null leaves every fire on the stock timing.</summary>
    public static Func<Mobile, CampfireTiming> TimingProvider { get; set; }

    public Mobile Lighter { get; private set; }

    public CampfireTiming Timing { get; private set; }

    public DateTime LitAt { get; private set; }

    /// <summary>When the fire was first lit. Unlike <see cref="LitAt" />, feeding does not move it.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Burns the fire up again from now, as a fire lit with <paramref name="timing" />. A longer timing the fire already
    /// has is kept, so a weaker feeder cannot shorten it. Embers light again.
    /// </summary>
    public void Feed(CampfireTiming timing)
    {
        if (Deleted)
        {
            return;
        }

        if (timing.Expire > Timing.Expire)
        {
            Timing = timing;
        }

        LitAt = Core.Now;
        Status = CampfireStatus.Burning;
    }

    public override bool SkipSerialization => true;

    [CommandProperty(AccessLevel.GameMaster)]
    public CampfireStatus Status
    {
        get
        {
            return ItemID switch
            {
                0xDE3 => CampfireStatus.Burning,
                0xDE9 => CampfireStatus.Extinguishing,
                _     => CampfireStatus.Off
            };
        }
        set
        {
            if (Status == value)
            {
                return;
            }

            switch (value)
            {
                case CampfireStatus.Burning:
                    {
                        ItemID = 0xDE3;
                        Light = LightType.Circle300;
                        break;
                    }

                case CampfireStatus.Extinguishing:
                    {
                        ItemID = 0xDE9;
                        Light = LightType.Circle150;
                        break;
                    }

                default:
                    {
                        ItemID = 0xDEA;
                        Light = LightType.ArchedWindowEast;
                        ClearEntries();
                        break;
                    }
            }
        }
    }

    public static CampfireEntry GetEntry(Mobile player) => _table.GetValueOrDefault(player);

    public static void RemoveEntry(CampfireEntry entry)
    {
        _table.Remove(entry.Player);
        entry.Fire._entries.Remove(entry);
    }

    private void OnTick()
    {
        var now = Core.Now;

        if (Timing.StatusAt(now - LitAt) is { } status)
        {
            Status = status;
        }
        else
        {
            Delete();
        }

        if (Status == CampfireStatus.Off || Deleted)
        {
            return;
        }

        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];

            if (!entry.Valid || entry.Player.NetState == null)
            {
                RemoveEntry(entry);
            }
            else if (!entry.Safe && now - entry.Start >= TimeSpan.FromSeconds(30.0))
            {
                entry.Safe = true;
                entry.Player.SendLocalizedMessage(500621); // The camp is now secure.
            }
        }

        foreach (var state in GetClientsInRange(SecureRange))
        {
            if (state.Mobile is PlayerMobile pm && GetEntry(pm) == null)
            {
                var entry = new CampfireEntry(pm, this);

                _table[pm] = entry;
                _entries.Add(entry);

                pm.SendLocalizedMessage(500620); // You feel it would take a few moments to secure your camp.
            }
        }
    }

    private void ClearEntries()
    {
        if (_entries == null)
        {
            return;
        }

        foreach (var entry in _entries)
        {
            _table.Remove(entry.Player);
        }

        _entries.Clear();
        _entries.TrimExcess();
    }

    public override void OnAfterDelete()
    {
        _timerToken.Cancel();
        ClearEntries();
        _active.Remove(this);
        Lighter = null;
    }
}

public class CampfireEntry
{
    private bool _safe;

    public CampfireEntry(PlayerMobile player, Campfire fire)
    {
        Player = player;
        Fire = fire;
        Start = Core.Now;
        _safe = false;
    }

    public PlayerMobile Player { get; }
    public Campfire Fire { get; }
    public DateTime Start { get; }

    public bool Valid => !Fire.Deleted && Fire.Status != CampfireStatus.Off && Player.Map == Fire.Map &&
                         Player.InRange(Fire, Campfire.SecureRange);

    public bool Safe
    {
        get => Valid && _safe;
        set => _safe = value;
    }
}
