
namespace Core.GOAP
{
    public class GoapAgentState
    {
        public bool NeedLoot { get; set; } = false;
        public bool NeedSkin { get; set; } = false;

        #region Last Combat Kill Count

        public int LastCombatKillCount { get; private set; } = 0;

        public void IncrementKillCount()
        {
            LastCombatKillCount++;
        }

        public void DecrementKillCount()
        {
            LastCombatKillCount--;
            if (LastCombatKillCount < 0)
            {
                LastCombatKillCount = 0;
            }
        }

        #endregion

        #region Corpse Consumption

        public bool ShouldConsumeCorpse { get; private set; } = false;

        public void ProduceCorpse()
        {
            ShouldConsumeCorpse = true;
        }

        public void ConsumeCorpse()
        {
            ShouldConsumeCorpse = false;
        }

        #endregion
    }
}
