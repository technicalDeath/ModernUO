using System;
using ModernUO.Serialization;
using Server.Engines.ConPVP;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Fists : BaseMeleeWeapon
    {
        public Fists() : base(0)
        {
            Visible = false;
            Movable = false;
            Quality = WeaponQuality.Regular;
        }

        public override bool SkipSerialization => true;

        public override WeaponAbility PrimaryAbility => WeaponAbility.Disarm;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;

        public override int AosStrengthReq => 0;
        public override int AosMinDamage => 1;
        public override int AosMaxDamage => 4;
        public override int AosSpeed => 50;
        public override float MlSpeed => 2.50f;

        public override int OldStrengthReq => 0;
        public override int OldMinDamage => 1;
        public override int OldMaxDamage => 8;
        public override int OldSpeed => 30;

        public override int DefHitSound => -1;
        public override int DefMissSound => -1;

        public override SkillName DefSkill => SkillName.Wrestling;
        public override WeaponType DefType => WeaponType.Fists;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Wrestle;

        public static void Initialize()
        {
            Mobile.DefaultWeapon = new Fists();
        }

        public override double GetDefendSkillValue(Mobile attacker, Mobile defender)
        {
            var wresValue = defender.Skills.Wrestling.Value;

            if (!Core.LBR)
            {
                return wresValue;
            }

            var anatValue = defender.Skills.Anatomy.Value;
            var evalValue = defender.Skills.EvalInt.Value;
            var incrValue = Math.Min((anatValue + evalValue + 20.0) * 0.5, 120.0);

            return wresValue > incrValue ? wresValue : incrValue;
        }

        private static void ClearDisabledPreAosMoves(Mobile attacker)
        {
            // Britannia Renaissance retains ordinary Wrestling, but never permits the UOR
            // Stun/Disarm ready states to resolve (including states persisted before this gate).
            attacker.StunReady = false;
            attacker.DisarmReady = false;
        }

        public override TimeSpan OnSwing(Mobile attacker, Mobile defender, double damageBonus = 1.0)
        {
            ClearDisabledPreAosMoves(attacker);

            return base.OnSwing(attacker, defender);
        }

        /*public override void OnMiss( Mobile attacker, Mobile defender )
        {
          base.PlaySwingAnimation( attacker );
        }*/

        public static void DisarmRequest(Mobile m)
        {
            // Request gate: packet and AI callers can never arm the removed UOR move.
            m.DisarmReady = false;
            m.SendMessage("Wrestling Disarm is disabled on this shard.");
        }

        public static void StunRequest(Mobile m)
        {
            // Request gate: packet and AI callers can never arm the removed UOR move.
            m.StunReady = false;
            m.SendMessage("Wrestling Stun is disabled on this shard.");
        }
    }
}
