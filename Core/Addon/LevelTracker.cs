using System;

namespace Core
{
    public class LevelTracker
    {
        private readonly object dummyLock = new object();
        private readonly PlayerReader playerReader;
        private WowPoint corpseLocation = new WowPoint(-1, -1);

        private int level = 0;
        private int lastXp = 0;

        private DateTime levelStartTime = DateTime.Now;
        private int levelStartXP = 0;

        public DateTime PredictedLevelTime { get; private set; } = DateTime.Now;
        public int MobsKilled { get; private set; }
        public int Death { get; set; }
        public string TimeToLevel { get; private set; } = string.Empty;

        public LevelTracker(PlayerReader playerReader)
        {
            this.playerReader = playerReader;
        }

        public void ResetMobsKilled()
        {
            MobsKilled = 0;
        }

        public void ResetDeath()
        {
            Death = 0;
        }

        public void Update()
        {
            lock (dummyLock)
            {
                if (playerReader.PlayerBitValues.DeadStatus &&
                    !corpseLocation.Equals(playerReader.CorpseLocation) &&
                    !playerReader.CorpseLocation.Equals(new WowPoint(0,0)))
                {
                    corpseLocation = playerReader.CorpseLocation;
                    Death++;
                }
                if (level != playerReader.PlayerLevel)
                {
                    level = playerReader.PlayerLevel;
                    lastXp = playerReader.PlayerXp;
                    levelStartTime = DateTime.Now;
                    levelStartXP = playerReader.PlayerXp;
                }
                else
                {
                    if (lastXp != playerReader.PlayerXp)
                    {
                        lastXp = playerReader.PlayerXp;
                        MobsKilled++;

                        var runningSeconds = (DateTime.Now - levelStartTime).TotalSeconds;
                        var xpPerSecond = (playerReader.PlayerXp - levelStartXP) / runningSeconds;
                        var secondsLeft = (playerReader.PlayerMaxXp - playerReader.PlayerXp) / xpPerSecond;

                        TimeToLevel = new TimeSpan(0, 0, (int)secondsLeft).ToString();

                        if (secondsLeft > 0 && secondsLeft < 60 * 60 * 10)
                        {
                            PredictedLevelTime = DateTime.Now.AddSeconds(secondsLeft);
                        }
                    }
                }
            }
        }
    }
}