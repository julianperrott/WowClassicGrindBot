
namespace Core.GOAP
{
    public class GoapAgentState
    {
        public bool ShouldConsumeCorpse { get; set; } = false;

        public bool NeedLoot { get; set; } = false;
        public bool NeedSkin { get; set; } = false;

        public int LastCombatKillCount { get; private set; } = 0;

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
