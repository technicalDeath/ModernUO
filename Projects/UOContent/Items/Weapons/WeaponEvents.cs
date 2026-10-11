using System;
using System.Runtime.CompilerServices;

namespace Server.Items;

/// <summary>
/// Shard hooks into the pre-AOS weapon formulas. Each is null by default, which is the stock behavior, and each takes the stock
/// value in and returns the value to use, so subscribers chain the previous delegate. They are plain delegates because generated
/// events cannot be subscribed across assemblies.
/// </summary>
public static class WeaponEvents
{
    /// <summary>
    /// Arguments: the attacker, the attacker's weapon skill value, the stock hit chance of the swing. Returns the hit chance the
    /// shard wants the swing to have (at least the stock chance to take effect). Consulted only after the stock skill check has
    /// missed, as a second roll with probability <c>(returned - stock) / (1 - stock)</c>, so the stock skill check, and with it the
    /// skill-gain odds, is unchanged.
    /// </summary>
    public static Func<Mobile, double, double, double> HitChance { get; set; }

    /// <summary>
    /// Arguments: the attacker, the attacker's Tactics value. Returns the Tactics value the pre-AOS damage modifier uses
    /// (<c>(tactics - 50) / 100</c>); the skill itself is untouched.
    /// </summary>
    public static Func<Mobile, double, double> TacticsForDamage { get; set; }

    /// <summary>
    /// Arguments: the swinger, its current stamina. Returns the stamina the pre-AOS swing delay uses
    /// (<c>15000 / ((stamina + 100) x speed)</c> seconds); the stamina itself is untouched.
    /// </summary>
    public static Func<Mobile, int, int> StaminaForSwingDelay { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetHitChance(Mobile attacker, double skillValue, double stockChance) =>
        HitChance?.Invoke(attacker, skillValue, stockChance) ?? stockChance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetTacticsForDamage(Mobile attacker, double tactics) =>
        TacticsForDamage?.Invoke(attacker, tactics) ?? tactics;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStaminaForSwingDelay(Mobile swinger, int stamina) =>
        StaminaForSwingDelay?.Invoke(swinger, stamina) ?? stamina;
}
