using System.Collections.Generic;
using System.IO;
using Libs.Addon;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Libs.Database
{
    public class CreatureDB
    {
        private readonly ILogger logger;
        private readonly DataConfig dataConfig;

        public Dictionary<int, Creature> Entries { get; } = new Dictionary<int, Creature>();

        public CreatureDB(ILogger logger, DataConfig dataConfig)
        {
            this.logger = logger;
            this.dataConfig = dataConfig;

            var creatures = JsonConvert.DeserializeObject<List<Creature>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "creatures.json")));
            creatures.ForEach(i => Entries.Add(i.Entry, i));
        }

    }
}
