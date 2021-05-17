using System;
using System.Collections.Generic;

namespace Core.Session
{
    public interface IGrindSession
    {
        Guid SessionId { get; set; }
        string PathName { get; set; }
        PlayerClassEnum PlayerClass { get; set; }
        DateTime SessionStart { get; set; }
        DateTime SessionEnd { get; set; }
        int LevelFrom { get; set; }
        double XpFrom { get; set; }
        int LevelTo { get; set; }
        double XpTo { get; set; }
        int MobsKilled { get; set; }
        void StartBotSession();
        void StopBotSession(string reason = "stopped by player");
        void Save();
        List<GrindSession> Load();
    }
}
