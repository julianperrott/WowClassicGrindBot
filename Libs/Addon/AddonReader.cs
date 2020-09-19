using Libs.Addon;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Libs
{
    public class AddonReader : IAddonReader
    {
        public List<DataFrame> frames { get; private set; } = new List<DataFrame>();
        private Color[] FrameColor { get; set; } = new Color[200];

        private readonly ILogger logger;
        private readonly ISquareReader squareReader;
        public PlayerReader PlayerReader { get; set; }
        public BagReader BagReader { get; set; }
        public EquipmentReader equipmentReader { get; set; }
        public bool Active { get; set; } = true;
        public LevelTracker LevelTracker { get; set; }

        public event EventHandler? AddonDataChanged;

        private readonly int width;
        private readonly int height;
        private readonly IColorReader colorReader;

        public AddonReader(IColorReader colorReader, List<DataFrame> frames, ILogger logger)
        {
            this.frames = frames;
            this.logger = logger;
            this.colorReader = colorReader;
            this.width = frames.Last().point.X + 1;
            this.height = frames.Max(f => f.point.Y) + 1;
            this.squareReader = new SquareReader(this);

            var items = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText("../json/data/items.json"));
            var creatures = JsonConvert.DeserializeObject<List<Creature>>(File.ReadAllText("../json/data/creatures.json"));

            this.BagReader = new BagReader(squareReader, 20, items, ReadAHPrices());
            this.equipmentReader = new EquipmentReader(squareReader, 30);
            this.PlayerReader = new PlayerReader(squareReader, creatures);
            this.LevelTracker = new LevelTracker(PlayerReader);
        }

        private Dictionary<int, int> ReadAHPrices()
        {
            var auctionHouseDictionary = new Dictionary<int, int>();

            if (File.Exists(@"D:\GitHub\Auc-Stat-Simple.lua.location"))
            {
                try
                {
                    // file will contain be something like D:\World of Warcraft Classic\World of Warcraft\_classic_\WTF\Account\XXXXXXX\SavedVariables\Auc-Stat-Simple.lua
                    var filename = File.ReadAllLines(@"D:\GitHub\Auc-Stat-Simple.lua.location").FirstOrDefault();
                    File.ReadAllLines(filename)
                        .Select(l => l.Trim())
                        .Where(l => l.StartsWith("["))
                        .Where(l => l.Contains(";"))
                        .Where(l => l.Contains("="))
                        .Select(Process)
                        .GroupBy(l => l.Key)
                        .Select(g => g.First())
                        .ToList()
                        .ForEach(i => auctionHouseDictionary.Add(i.Key, i.Value));
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Failed to read AH prices." + ex.Message);
                }
            }
            else
            {
                this.logger.LogWarning("AH prices unavailable, don't worry about this message!");
            }

            return auctionHouseDictionary;
        }

        public static KeyValuePair<int, int> Process(string line)
        {
            var parts = line.Split("=");
            var id = int.Parse(parts[0].Replace("[", "").Replace("]", "").Replace("\"", "").Trim());
            var valueParts = parts[1].Split("\"")[1].Split(";");

            double value = 0;
            if (valueParts.Length == 3 || valueParts.Length == 6)
            {
                value = double.Parse(valueParts[valueParts.Length - 1]);
            }
            else if (valueParts.Length == 4 || valueParts.Length == 7)
            {
                value = double.Parse(valueParts[valueParts.Length - 2]);
            }

            return new KeyValuePair<int, int>(id, (int)value);
        }

        private int seq = 0;

        public void AddonRefresh()
        {
            Refresh();

            // 20 - 29
            var bagItems = BagReader.Read();

            // 30 - 31
            var equipment = equipmentReader.Read();

            LevelTracker.Update();

            seq++;

            if (seq >= 10)
            {
                seq = 0;
                AddonDataChanged?.Invoke(this, new EventArgs());
            }
            System.Threading.Thread.Sleep(10);
        }

        public void Refresh()
        {
            try
            {
                using (var bitMap = WowScreen.GetAddonBitmap(this.width, this.height))
                {
                    frames.ForEach(frame => FrameColor[frame.index] = colorReader.GetColorAt(frame.point, bitMap));
                }

                if (PlayerReader != null)
                {
                    PlayerReader.Updated();
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex.Message);
            }
        }

        public Color GetColorAt(int index)
        {
            return FrameColor[index];
        }
    }
}