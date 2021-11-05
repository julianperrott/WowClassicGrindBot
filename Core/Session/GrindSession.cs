using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Core.Session
{
    public class GrindSession : IGrindSession
    {
        private readonly IBotController _botController;
        private readonly IGrindSessionHandler _grindSessionHandler;
        private Thread? _autoUpdateThread;

        public GrindSession(IBotController botController, IGrindSessionHandler grindSessionHandler)
        {
            _botController = botController;
            _grindSessionHandler = grindSessionHandler;
        }
        [JsonIgnore] 
        public bool Active { get; set; }
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
        public double MobsPerMinute => Math.Round(MobsKilled / (double)TotalTimeInMinutes, 2);
        public int Death { get; set; }
        public string? Reason { get; set; }
        [JsonIgnore]
        public double ExperiencePerHour => TotalTimeInMinutes == 0 ? 0 : Math.Round((double)(ExpGetInBotSession / TotalTimeInMinutes * 60), 0);
        [JsonIgnore]
        public double ExpGetInBotSession
        {
            get
            {
                var expList = ExperienceProvider.GetExperienceList();
                var maxLevel = expList.Length + 1;
                if (LevelFrom == maxLevel)
                    return 0;

                if (LevelFrom == maxLevel-1 && LevelTo == maxLevel)
                    return expList[LevelFrom - 1] - XpFrom;

                if (LevelTo == LevelFrom)
                {
                    return XpTo - XpFrom;
                }

                if (LevelTo > LevelFrom)
                {
                    var expSoFar = XpTo;

                    for (int i = 0; i < LevelTo-LevelFrom; i++)
                    {
                        expSoFar += expList[LevelFrom - 1 + i] - XpFrom;
                        XpFrom = 0;
                        if (LevelTo > maxLevel)
                            break;
                    }

                    return expSoFar;
                }

                return 0;
            }
        }

        public void StartBotSession()
        {
            Active = true;
            SessionId = Guid.NewGuid();
            PathName = _botController.SelectedPathFilename ?? _botController.ClassConfig?.PathFilename ?? "No Path Selected";
            PlayerClass = _botController.AddonReader.PlayerReader.Class;
            SessionStart = DateTime.UtcNow;
            LevelFrom = _botController.AddonReader.PlayerReader.Level.Value;
            XpFrom = _botController.AddonReader.PlayerReader.PlayerXp.Value;
            MobsKilled = _botController.AddonReader.LevelTracker.MobsKilled;
            _autoUpdateThread = new Thread(() => Task.Factory.StartNew(AutoUpdate));
            _autoUpdateThread.Start();
        }

        private async Task AutoUpdate()
        {
            if (_autoUpdateThread != null)
            {
                while (Active)
                {
                    StopBotSession("auto save", true);
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
        }

        public void StopBotSession(string reason = "stopped by player", bool active = false)
        {
            Active = active;
            SessionEnd = DateTime.UtcNow;
            LevelTo = _botController.AddonReader.PlayerReader.Level.Value;
            XpTo = _botController.AddonReader.PlayerReader.PlayerXp.Value;
            Reason = reason;
            Death = _botController.AddonReader.LevelTracker.Death;
            MobsKilled = _botController.AddonReader.LevelTracker.MobsKilled;
            Save();
        }
        
        public void Save()
        {
            _grindSessionHandler.Save(this);
        }

        public List<GrindSession> Load()
        {
            return _grindSessionHandler.Load();
        }
    }
}
