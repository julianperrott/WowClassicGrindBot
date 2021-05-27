using System.Collections.Generic;
using System.IO;
using Core.Addon;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Core.Database
{
    public class ItemDB
    {
        private readonly ILogger logger;
        private readonly DataConfig dataConfig;

        public Dictionary<int, Item> Items { get; } = new Dictionary<int, Item>();
        public HashSet<int> FoodIds { get; } = new HashSet<int>();
        public HashSet<int> WaterIds { get; } = new HashSet<int>();

        public HashSet<int> ContainerIds { get; } = new HashSet<int>();


        public ItemDB(ILogger logger, DataConfig dataConfig)
        {
            this.logger = logger;
            this.dataConfig = dataConfig;

            var items = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "items.json")));
            items.ForEach(i =>
            {
                i.AHPrice = i.SellPrice;
                Items.Add(i.Entry, i);
            });

            AuctionHouse ah = new AuctionHouse(logger, dataConfig, this);
            var ahPrices = ah.ReadAHPrices();
            foreach (KeyValuePair<int, int> entry in ahPrices)
            {
                Items[entry.Key].AHPrice = entry.Value;
            }

            var foods = JsonConvert.DeserializeObject<List<ItemId>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "foods.json")));
            foods.ForEach(x => FoodIds.Add(x.Id));

            var waters = JsonConvert.DeserializeObject<List<ItemId>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "waters.json")));
            waters.ForEach(x => WaterIds.Add(x.Id));
        }

        public Item? Get(string name)
        {
            foreach(var kvp in Items)
            {
                if (kvp.Value.Name == name)
                    return Items[kvp.Key];
            }

            return null;
        }
    }
}
