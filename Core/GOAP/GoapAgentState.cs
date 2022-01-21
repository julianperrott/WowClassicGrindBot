
namespace Core.GOAP
{
    public class GoapAgentState
    {
        public bool ShouldConsumeCorpse { get; set; }

        public bool NeedLoot { get; set; }
        public bool NeedSkin { get; set; }

        public int LastCombatKillCount { get; private set; }

        public void IncKillCount()
        {
            LastCombatKillCount++;
        }

        public void DecKillCount()
        {
            LastCombatKillCount--;
            if (LastCombatKillCount < 0)
            {
                LastCombatKillCount = 0;
            }
        }
    }
}
