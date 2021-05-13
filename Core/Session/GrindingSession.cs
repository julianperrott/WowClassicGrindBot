using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.Session
{
    public class GrindingSession : IGrindingSession
    {
        private readonly IBotController _botController;

        private readonly IGrindingSessionHandler _grindingSessionHandler;

        // this will change for TBC
        // we might need to consider getting Wow client version and determine the calculation from there
        private readonly double[] _experience =
        {
            400, 900, 1400, 2100, 2800, 3600, 4500, 5400, 6500, 7600, 8800, 10100, 11400, 12900, 14400, 16000, 17700,
            19400, 21300, 23200, 25200, 27300, 29400, 31700, 34000, 36400, 38900, 41400, 44300, 47400, 50800, 54700,
            58600, 62800, 67000, 71600, 76100, 80800, 85700, 90700, 95800, 101000, 106300, 111800, 117400, 123200,
            129100, 135100, 141200, 147500, 153900, 160400, 167100, 173900, 180800, 187900, 195000, 202300, 209800
        };

        public GrindingSession(IBotController botController, IGrindingSessionHandler grindingSessionHandler)
        {
            _botController = botController;
            _grindingSessionHandler = grindingSessionHandler;
        }

        public Guid SessionId { get; set; }
        public string PathName { get; set; } = "No path selected";
        public PlayerClassEnum PlayerClass { get; set; }
        public DateTime SessionStart { get; set; }
        public DateTime SessionEnd { get; set; }
        [JsonIgnore]
        public int TotalTimeInMinutes => (int)(SessionEnd - SessionStart).TotalMinutes;
        public int LevelFrom { get; set; }
        public double XpFrom { get; set; }
        public int LevelTo { get; set; }
        public double XpTo { get; set; }
        public int MobsKilled { get; set; }
        [JsonIgnore]
        public double ExperiencePerHour => TotalTimeInMinutes == 0 ? 0 : Math.Round((double)(ExpGetInBotSession / TotalTimeInMinutes * 60), 0);
        [JsonIgnore]
        public double ExpGetInBotSession
        {
            get
            {
                if (LevelFrom == 60)
                    return 0;

                if (LevelFrom == 59 && LevelTo == 60)
                    return _experience[LevelFrom - 1] - XpFrom;

                if (LevelTo == LevelFrom)
                {
                    return XpTo - XpFrom;
                }

                if (LevelTo > LevelFrom)
                {
                    var expSoFar = XpTo;

                    for (int i = 0; i < LevelTo-LevelFrom; i++)
                    {
                        expSoFar += _experience[LevelFrom - 1 + i] - XpFrom;
                        XpFrom = 0;
                        if (LevelTo > 60)
                            break;
                    }

                    return expSoFar;
                }

                return 0;
            }
        }

        public void StartBotSession()
        {
            SessionId = Guid.NewGuid();
            PathName = _botController.SelectedPathFilename ?? _botController.ClassConfig?.PathFilename ?? "No Path Selected";
            PlayerClass = _botController.AddonReader.PlayerReader.PlayerClass;
            SessionStart = DateTime.UtcNow;
            LevelFrom = (int)_botController.AddonReader.PlayerReader.PlayerLevel;
            XpFrom = (int)_botController.AddonReader.PlayerReader.PlayerXp;
            MobsKilled = (int)_botController.AddonReader.LevelTracker.MobsKilled;
        }

        public void StopBotSession()
        {
            SessionEnd = DateTime.UtcNow;
            LevelTo = (int)_botController.AddonReader.PlayerReader.PlayerLevel;
            XpTo = (int)_botController.AddonReader.PlayerReader.PlayerXp;
            MobsKilled = (int)_botController.AddonReader.LevelTracker.MobsKilled;
            Save();
        }

        public void Save()
        {
            _grindingSessionHandler.Save(this);
        }

        public List<GrindingSession> Load()
        {
            return _grindingSessionHandler.Load();
        }
    }
}
