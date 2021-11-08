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
        float XpFrom { get; set; }
        int LevelTo { get; set; }
        float XpTo { get; set; }
        int MobsKilled { get; set; }
        void StartBotSession();
        void StopBotSession(string reason, bool active);
        void Save();
        List<GrindSession> Load();
    }
}
