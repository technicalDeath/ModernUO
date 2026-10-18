using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseRefreshPotion : BasePotion
{
    public BaseRefreshPotion(PotionEffect effect) : base(0xF0B, effect)
    {
    }

    /// <summary>Raised after a refresh potion is drunk: the drinker and the stamina it really gained.</summary>
    public static event System.Action<Mobile, int> StaminaRestored;

    public abstract double Refresh { get; }

    public override bool CanDrink(Mobile from)
    {
        if (!base.CanDrink(from))
        {
            return false;
        }

        if (from.Stam >= from.StamMax)
        {
            from.SendMessage("You decide against drinking this potion, as you are already at full stamina.");
            return false;
        }

        return true;
    }

    public override void Drink(Mobile from)
    {
        var before = from.Stam;
        from.Stam += Scale(from, (int)(Refresh * from.StamMax));

        if (from.Stam > before)
        {
            StaminaRestored?.Invoke(from, from.Stam - before);
        }

        PlayDrinkEffect(from);
    }
}
