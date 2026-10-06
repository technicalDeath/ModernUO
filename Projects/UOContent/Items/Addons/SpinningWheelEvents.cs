using System;

namespace Server.Items;

/// <summary>
///     Raised by the spinnable materials (wool, cotton, flax) as a spin begins and as its yarn or thread lands in the pack,
///     so a distribution can react without replacing them. The material may already be deleted when the spin ends, if it
///     was the last of its stack.
/// </summary>
public static class SpinningWheelEvents
{
    public static event Action<Item, ISpinningWheel, Mobile> SpinStarted;

    public static event Action<Item, ISpinningWheel, Mobile> Spun;

    public static void InvokeSpinStarted(Item material, ISpinningWheel wheel, Mobile from) =>
        SpinStarted?.Invoke(material, wheel, from);

    public static void InvokeSpun(Item material, ISpinningWheel wheel, Mobile from) => Spun?.Invoke(material, wheel, from);
}
