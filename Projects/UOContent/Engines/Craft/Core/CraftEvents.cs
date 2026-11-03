using System;

namespace Server.Engines.Craft;

/// <summary>
///     Raised as a craft starts (with how long until its result), when it is cut short before then, and as a made item goes into the
///     crafter's pack, so a distribution can react without replacing the craft system.
/// </summary>
public static class CraftEvents
{
    public static event Action<Mobile, CraftSystem, CraftItem, TimeSpan> CraftStarted;

    public static event Action<Mobile, CraftSystem> CraftCancelled;

    /// <summary>The item made, raised before it goes into the pack (where a pile would merge into one already carried).</summary>
    public static event Action<Mobile, CraftSystem, Item> Crafted;

    /// <summary>The craft timer ticks at once and then every <see cref="CraftSystem.Delay"/>; the result comes at the last tick.</summary>
    public static TimeSpan CraftDuration(CraftSystem system, int ticks) => TimeSpan.FromSeconds(system.Delay * Math.Max(ticks - 1, 0));

    internal static void InvokeCraftStarted(Mobile from, CraftSystem system, CraftItem item, int ticks) =>
        CraftStarted?.Invoke(from, system, item, CraftDuration(system, ticks));

    internal static void InvokeCraftCancelled(Mobile from, CraftSystem system) => CraftCancelled?.Invoke(from, system);

    internal static void InvokeCrafted(Mobile from, CraftSystem system, Item item) => Crafted?.Invoke(from, system, item);
}
