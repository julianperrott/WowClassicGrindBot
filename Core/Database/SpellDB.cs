using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Core.Database
{
    public class SpellDB
    {
        public Dictionary<int, Spell> Spells { get; } = new Dictionary<int, Spell>();

        public SpellDB(ILogger logger, DataConfig dataConfig)
        {
            var items = JsonConvert.DeserializeObject<List<Spell>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "spells.json")));
            items.ForEach(i =>
            {
                Spells.Add(i.Id, i);
            });
        }
    }
}
