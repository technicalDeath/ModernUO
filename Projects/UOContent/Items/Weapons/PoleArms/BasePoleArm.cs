using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Harvest;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BasePoleArm : BaseMeleeWeapon, IUsesRemaining
    {
        [SerializableField(0)]
        [InvalidateProperties]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _showUsesRemaining;

        [SerializableField(1)]
        [InvalidateProperties]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _usesRemaining;

        public BasePoleArm(int itemID) : base(itemID) => _usesRemaining = 150;

        public override int DefHitSound => 0x237;
        public override int DefMissSound => 0x238;

        public override SkillName DefSkill => SkillName.Swords;
        public override WeaponType DefType => WeaponType.Polearm;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Slash2H;

        public virtual HarvestSystem HarvestSystem => Lumberjacking.System;

        public override void OnDoubleClick(Mobile from)
        {
            if (HarvestSystem == null)
            {
                return;
            }

            if (IsChildOf(from.Backpack) || Parent == from)
            {
                HarvestSystem.BeginHarvesting(from, this);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);

            if (HarvestSystem != null)
            {
                BaseHarvestTool.AddContextMenuEntries(from, this, ref list, HarvestSystem);
            }
        }

    }
}
