using System;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class CookableFood : Item
{
    private static readonly TimeSpan CookDelay = TimeSpan.FromSeconds(5.0);

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _cookingLevel;

    public CookableFood(int itemID, int cookingLevel) : base(itemID) => _cookingLevel = cookingLevel;

    public abstract Food Cook();

    // Before the Publish 14 cooking menu, cooking was this: use the food, then target a heat source.
    public override void OnDoubleClick(Mobile from)
    {
        if (!CanCook(from))
        {
            return;
        }

        from.Target = new CookTarget(this);
    }

    // Rechecked when the heat source is picked, so food handed away in between cannot be cooked from afar.
    private bool CanCook(Mobile from)
    {
        if (Deleted || !VerifyMove(from) || RootParent is Mobile owner && owner != from)
        {
            return false;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return false;
        }

        return true;
    }

    private static void FinishCooking(Mobile from, IPoint3D heatSource, Map map, CookableFood food)
    {
        from.EndAction<CookableFood>();

        if (from.Deleted || from.Map != map || heatSource != null && from.GetDistanceToSqrt(heatSource) > 3)
        {
            from.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
            return;
        }

        // The era chance is the Cooking skill itself and every food can be tried at zero skill, so the
        // per-food CookingLevel (a RunUO minimum for the menu) is not used.
        if (!from.CheckSkill(SkillName.Cooking, 0.0, 100.0))
        {
            from.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
            return;
        }

        if (from.AddToBackpack(food.Cook()))
        {
            from.PlaySound(0x57);
        }
    }

    private class CookTarget : Target
    {
        private readonly CookableFood _food;

        public CookTarget(CookableFood food) : base(1, false, TargetFlags.None) => _food = food;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!IsHeatSource(targeted) || !_food.CanCook(from))
            {
                return;
            }

            if (!from.BeginAction<CookableFood>())
            {
                from.SendLocalizedMessage(500119); // You must wait to perform another action.
                return;
            }

            from.PlaySound(0x225);

            // One item per cook, even from a stack; Consume may delete the stack, which Cook() does not care about.
            var food = _food;
            var heatSource = targeted as IPoint3D;
            var map = from.Map;
            food.Consume();

            Timer.StartTimer(CookDelay, () => FinishCooking(from, heatSource, map, food));
        }
    }

    public static bool IsHeatSource(object targeted) =>
        targeted switch
        {
            Item item           => IsHeatSource(item.ItemID),
            StaticTarget target => IsHeatSource(target.ItemID),
            _                   => false
        };

    public static bool IsHeatSource(int itemID)
    {
        if (itemID >= 0xDE3 && itemID <= 0xDE9)
        {
            return true; // Campfire
        }

        if (itemID >= 0x461 && itemID <= 0x48E)
        {
            return true; // Sandstone oven/fireplace
        }

        if (itemID >= 0x92B && itemID <= 0x96C)
        {
            return true; // Stone oven/fireplace
        }

        if (itemID == 0xFAC)
        {
            return true; // Firepit
        }

        if (itemID >= 0x184A && itemID <= 0x184C)
        {
            return true; // Heating stand (left)
        }

        if (itemID >= 0x184E && itemID <= 0x1850)
        {
            return true; // Heating stand (right)
        }

        if (itemID >= 0x398C && itemID <= 0x399F)
        {
            return true; // Fire field
        }

        // Era cooks used forges, and the craft menu's own heat list includes them.
        if (itemID >= 0x197A && itemID <= 0x19A9)
        {
            return true; // Large forge
        }

        return itemID == 0xFB1; // Small forge
    }
}

[SerializationGenerator(0, false)]
public partial class RawRibs : CookableFood
{
    [Constructible]
    public RawRibs(int amount = 1) : base(0x9F1, 10)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 1.0;

    public override Food Cook() => new Ribs();
}

[SerializationGenerator(0, false)]
public partial class RawLambLeg : CookableFood
{
    [Constructible]
    public RawLambLeg(int amount = 1) : base(0x1609, 10)
    {
        Stackable = true;
        Amount = amount;
    }

    public override Food Cook() => new LambLeg();
}

[SerializationGenerator(0, false)]
public partial class RawChickenLeg : CookableFood
{
    [Constructible]
    public RawChickenLeg() : base(0x1607, 10)
    {
        Stackable = true;
    }

    public override double DefaultWeight => 1.0;

    public override Food Cook() => new ChickenLeg();
}

[SerializationGenerator(0, false)]
public partial class RawBird : CookableFood
{
    [Constructible]
    public RawBird(int amount = 1) : base(0x9B9, 10)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 1.0;

    public override Food Cook() => new CookedBird();
}

[SerializationGenerator(0, false)]
public partial class UnbakedPeachCobbler : CookableFood
{
    [Constructible]
    public UnbakedPeachCobbler() : base(0x1042, 25)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041335; // unbaked peach cobbler

    public override Food Cook() => new PeachCobbler();
}

[SerializationGenerator(0, false)]
public partial class UnbakedFruitPie : CookableFood
{
    [Constructible]
    public UnbakedFruitPie() : base(0x1042, 25)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041334; // unbaked fruit pie

    public override Food Cook() => new FruitPie();
}

[SerializationGenerator(0, false)]
public partial class UnbakedMeatPie : CookableFood
{
    [Constructible]
    public UnbakedMeatPie() : base(0x1042, 25)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041338; // unbaked meat pie

    public override Food Cook() => new MeatPie();
}

[SerializationGenerator(0, false)]
public partial class UnbakedPumpkinPie : CookableFood
{
    [Constructible]
    public UnbakedPumpkinPie() : base(0x1042, 25)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041342; // unbaked pumpkin pie

    public override Food Cook() => new PumpkinPie();
}

[SerializationGenerator(0, false)]
public partial class UnbakedApplePie : CookableFood
{
    [Constructible]
    public UnbakedApplePie() : base(0x1042, 25)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041336; // unbaked apple pie

    public override Food Cook() => new ApplePie();
}

[TypeAlias("Server.Items.UncookedPizza")]
[SerializationGenerator(0, false)]
public partial class UncookedCheesePizza : CookableFood
{
    [Constructible]
    public UncookedCheesePizza() : base(0x1083, 20)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041341; // uncooked cheese pizza

    public override Food Cook() => new CheesePizza();
}

[SerializationGenerator(0, false)]
public partial class UncookedSausagePizza : CookableFood
{
    [Constructible]
    public UncookedSausagePizza() : base(0x1083, 20)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041337; // uncooked sausage pizza

    public override Food Cook() => new SausagePizza();
}

[SerializationGenerator(0, false)]
public partial class UnbakedQuiche : CookableFood
{
    [Constructible]
    public UnbakedQuiche() : base(0x1042, 25)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041339; // unbaked quiche

    public override Food Cook() => new Quiche();
}

[SerializationGenerator(0, false)]
public partial class Eggs : CookableFood
{
    [Constructible]
    public Eggs(int amount = 1) : base(0x9B5, 15)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 1.0;

    public override Food Cook() => new FriedEggs();
}

[SerializationGenerator(0, false)]
public partial class BrightlyColoredEggs : CookableFood
{
    [Constructible]
    public BrightlyColoredEggs() : base(0x9B5, 15)
    {
        Hue = 3 + Utility.Random(20) * 5;
    }

    public override double DefaultWeight => 0.5;
    public override string DefaultName => "brightly colored eggs";
    public override Food Cook() => new FriedEggs();
}

[SerializationGenerator(0, false)]
public partial class EasterEggs : CookableFood
{
    [Constructible]
    public EasterEggs() : base(0x9B5, 15)
    {
        Hue = 3 + Utility.Random(20) * 5;
    }

    public override double DefaultWeight => 0.5;
    public override int LabelNumber => 1016105; // Easter Eggs
    public override Food Cook() => new FriedEggs();
}

[SerializationGenerator(0, false)]
public partial class CookieMix : CookableFood
{
    [Constructible]
    public CookieMix() : base(0x103F, 20)
    {
    }

    public override double DefaultWeight => 1.0;

    public override Food Cook() => new Cookies();
}

[SerializationGenerator(0, false)]
public partial class CakeMix : CookableFood
{
    [Constructible]
    public CakeMix() : base(0x103F, 40)
    {
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1041002; // cake mix

    public override Food Cook() => new Cake();
}

[SerializationGenerator(0, false)]
public partial class RawFishSteak : CookableFood
{
    [Constructible]
    public RawFishSteak(int amount = 1) : base(0x097A, 10)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;

    public override Food Cook() => new FishSteak();
}
