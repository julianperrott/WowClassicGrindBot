using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Core.Session
{

    // this is gonna save the bot session data locally atm
    // there will be an AWS session handler later to upload the session data to AWS S3
    // the idea is we will have two session data handlers working at the same time
    public class LocalGrindingBotSessionHandler : IGrindingSessionHandler
    {
        private readonly string _historyPath;

        public LocalGrindingBotSessionHandler(string historyPath)
        {
            _historyPath = historyPath;
        }

        public List<GrindingSession> Load()
        {
            // first time load
            if (!Directory.Exists(_historyPath))
            {
                Directory.CreateDirectory(_historyPath);
                return new List<GrindingSession>();
            }
                
            var previousSessions =
                Directory.EnumerateFiles($"{_historyPath}", "*.json")
                    .Select(file => JsonConvert.DeserializeObject<GrindingSession>(File.ReadAllText(file)))
                    .OrderByDescending(grindingSession => grindingSession.SessionStart)
                    .ToList();

            return previousSessions;
        }

        public void Save(IGrindingSession grindingSession)
        {
            var json = JsonConvert.SerializeObject(grindingSession);
            if (!Directory.Exists(_historyPath))
                Directory.CreateDirectory(_historyPath);
            File.WriteAllText($@"{_historyPath}{grindingSession.SessionId}.json", json);
        }
    }
}