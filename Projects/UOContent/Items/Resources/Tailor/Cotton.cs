using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Cotton : Item, IDyable
{
    [Constructible]
    public Cotton(int amount = 1) : base(0xDF9)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 4.0;

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(502655); // What spinning wheel do you wish to spin this on?
            from.Target = new PickWheelTarget(this);
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public virtual void OnSpun(ISpinningWheel wheel, Mobile from, int hue)
    {
        from.AddToBackpack(new SpoolOfThread(6)
        {
            Hue = hue
        });
        from.SendLocalizedMessage(1010577); // You put the spools of thread in your backpack.
        SpinningWheelEvents.InvokeSpun(this, wheel, from);
    }

    // The cursor's reach; SpinOn checks it too, so a spin started without the cursor cannot reach further.
    private const int WheelRange = 3;

    /// <summary>The stock spin without the cursor: the target uses it, and so can anything repeating it.</summary>
    public bool SpinOn(Mobile from, ISpinningWheel wheel)
    {
        if (Deleted || wheel is not Item wheelItem)
        {
            return false;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return false;
        }

        if (!from.InRange(wheelItem.GetWorldLocation(), WheelRange))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return false;
        }

        if (wheel.Spinning)
        {
            from.SendLocalizedMessage(502656); // That spinning wheel is being used.
            return false;
        }

        Consume();
        wheel.BeginSpin(OnSpun, from, Hue);
        SpinningWheelEvents.InvokeSpinStarted(this, wheel, from);
        return true;
    }

    private class PickWheelTarget : Target
    {
        private readonly Cotton m_Cotton;

        public PickWheelTarget(Cotton cotton) : base(WheelRange, false, TargetFlags.None) => m_Cotton = cotton;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Cotton.Deleted)
            {
                return;
            }

            var wheel = targeted as ISpinningWheel;

            if (wheel == null && targeted is AddonComponent component)
            {
                wheel = component.Addon as ISpinningWheel;
            }

            if (wheel is not Item)
            {
                from.SendLocalizedMessage(502658); // Use that on a spinning wheel.
                return;
            }

            m_Cotton.SpinOn(from, wheel);
        }
    }
}
