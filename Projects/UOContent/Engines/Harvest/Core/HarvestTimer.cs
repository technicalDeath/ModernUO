using System;

namespace Server.Engines.Harvest
{
    public class HarvestTimer : Timer
    {
        private readonly int m_Count;
        private readonly HarvestDefinition m_Definition;
        private readonly Mobile m_From;
        private readonly object m_Locked;
        private readonly HarvestSystem m_System;
        private readonly object m_ToHarvest;
        private readonly Item m_Tool;
        private int m_Index;

        public HarvestTimer(
            Mobile from, Item tool, HarvestSystem system, HarvestDefinition def, object toHarvest,
            object locked
        ) : base(TimeSpan.Zero, def.EffectDelay)
        {
            m_From = from;
            m_Tool = tool;
            m_System = system;
            m_Definition = def;
            m_ToHarvest = toHarvest;
            m_Locked = locked;
            m_Count = def.EffectCounts.RandomElement();
        }

        /// <summary>How many swings this attempt makes; the result follows the last one's sound.</summary>
        public int Count => m_Count;

        protected override void OnTick()
        {
            var last = ++m_Index == m_Count;

            // the last swing returns false too: it hands the result to its sound timer (a failed check raises AttemptAborted itself)
            if (!m_System.OnHarvesting(m_From, m_Tool, m_Definition, m_ToHarvest, m_Locked, last))
            {
                Stop();
            }
        }
    }
}
