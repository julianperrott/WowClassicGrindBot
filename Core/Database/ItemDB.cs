using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedLib;

namespace Core.Database
{
    public class ItemDB
    {
        public Dictionary<int, Item> Items { get; } = new Dictionary<int, Item>();
        public HashSet<int> FoodIds { get; } = new HashSet<int>();
        public HashSet<int> WaterIds { get; } = new HashSet<int>();

        public HashSet<int> ContainerIds { get; } = new HashSet<int>();

        public ItemDB(ILogger logger, DataConfig dataConfig)
        {
            var items = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "items.json")));
            items.ForEach(i =>
            {
                Items.Add(i.Entry, i);
            });

            var foods = JsonConvert.DeserializeObject<List<EntityId>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "foods.json")));
            foods.ForEach(x => FoodIds.Add(x.Id));

            var waters = JsonConvert.DeserializeObject<List<EntityId>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "waters.json")));
            waters.ForEach(x => WaterIds.Add(x.Id));
        }
    }
}
