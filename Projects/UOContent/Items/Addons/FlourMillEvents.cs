using System;

namespace Server.Items;

/// <summary>
///     Raised by the flour mills as grinding begins and as a full sack of flour goes into the miller's pack, so a distribution can react
///     without replacing them.
/// </summary>
public static class FlourMillEvents
{
    /// <summary>How long a full mill takes to grind its wheat into a sack of flour.</summary>
    public static readonly TimeSpan GrindDelay = TimeSpan.FromSeconds(5.0);

    public static event Action<Mobile, Item> GrindStarted;

    public static event Action<Mobile, Item> FlourGiven;

    public static void InvokeGrindStarted(Mobile from, Item mill) => GrindStarted?.Invoke(from, mill);

    public static void InvokeFlourGiven(Mobile from, Item flour) => FlourGiven?.Invoke(from, flour);
}
