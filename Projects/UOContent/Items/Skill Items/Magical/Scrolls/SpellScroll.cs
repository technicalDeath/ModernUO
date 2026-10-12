using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Multis;
using Server.Network;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpellScroll : Item, ICommodity
{
    [SerializableField(0, setter: "private")]
    private int _spellID;

    [Constructible]
    public SpellScroll(int spellID, int itemID, int amount = 1) : base(itemID)
    {
        Stackable = true;
        Amount = amount;

        _spellID = spellID;
    }

    /// <summary>Text a shard adds after the scroll's name when a viewer clicks it once, or null for the stock label. Set once by shard code.</summary>
    public delegate string SingleClickPostfixDelegate(SpellScroll scroll, Mobile from);

    public static SingleClickPostfixDelegate SingleClickPostfixHandler { get; set; }

    public override double DefaultWeight => 1.0;

    public override void OnSingleClick(Mobile from)
    {
        var postfix = SingleClickPostfixHandler?.Invoke(this, from);
        var ns = from.NetState;

        // A scroll with its own name (not a client text number) or one that cannot be seen keeps the stock label.
        if (string.IsNullOrEmpty(postfix) || Name != null || Deleted || !from.CanSee(this) || ns.CannotSendPackets())
        {
            base.OnSingleClick(from);
            return;
        }

        if (DisplayLootType)
        {
            LabelLootTypeTo(from);
        }

        var affix = Amount > 1 ? $" : {Amount}{postfix}" : postfix;
        ns.SendMessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, LabelNumber, "", AffixType.Append, affix);
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from.Alive && Movable)
        {
            list.Add(new AddToSpellbookEntry());
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!DesignContext.Check(from))
        {
            return; // They are customizing
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var spell = SpellRegistry.NewSpell(_spellID, from, this);

        if (spell != null)
        {
            spell.Cast();
        }
        else
        {
            from.SendLocalizedMessage(502345); // This spell has been temporarily disabled.
        }
    }
}
