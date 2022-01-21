using System;

namespace Core
{
    public class LevelTracker
    {
        private readonly PlayerReader playerReader;

        private DateTime levelStartTime = DateTime.Now;
        private int levelStartXP;

        public string TimeToLevel { get; private set; } = "∞";
        public DateTime PredictedLevelUpTime { get; private set; } = DateTime.MaxValue;

        public int MobsKilled { get; private set; }
        public int Death { get; private set; }

        public LevelTracker(PlayerReader playerReader, EventHandler? playerDeath, CreatureHistory creatureHistory)
        {
            this.playerReader = playerReader;

            playerReader.Level.Changed -= PlayerLevel_Changed;
            playerReader.Level.Changed += PlayerLevel_Changed;

            playerReader.PlayerXp.Changed -= PlayerExp_Changed;
            playerReader.PlayerXp.Changed += PlayerExp_Changed;

            playerDeath -= OnPlayerDeath;
            playerDeath += OnPlayerDeath;

            creatureHistory.KillCredit -= OnKillCredit;
            creatureHistory.KillCredit += OnKillCredit;
        }

        public void Reset()
        {
            MobsKilled = 0;
            Death = 0;

            UpdateExpPerHour();
        }

        private void PlayerExp_Changed(object? sender, EventArgs e)
        {
            UpdateExpPerHour();
        }

        private void PlayerLevel_Changed(object? sender, EventArgs e)
        {
            levelStartTime = DateTime.Now;
            levelStartXP = playerReader.PlayerXp.Value;
        }

        private void OnPlayerDeath(object? sender, EventArgs e)
        {
            Death++;
        }

        private void OnKillCredit(object? sender, EventArgs e)
        {
            MobsKilled++;
        }

        public void UpdateExpPerHour()
        {
            var runningSeconds = (DateTime.Now - levelStartTime).TotalSeconds;
            var xpPerSecond = (playerReader.PlayerXp.Value - levelStartXP) / runningSeconds;
            var secondsLeft = (playerReader.PlayerMaxXp - playerReader.PlayerXp.Value) / xpPerSecond;

            if (xpPerSecond > 0)
            {
                TimeToLevel = new TimeSpan(0, 0, (int)secondsLeft).ToString();
            }
            else
            {
                TimeToLevel = "∞";
            }

            if (secondsLeft > 0 && secondsLeft < 60 * 60 * 10)
            {
                PredictedLevelUpTime = DateTime.Now.AddSeconds(secondsLeft);
            }
        }
    }
}