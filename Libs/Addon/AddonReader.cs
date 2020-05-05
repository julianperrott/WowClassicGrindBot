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
        public BagReader BagReader { get; private set; }
        public EquipmentReader equipmentReader { get; private set; }
        public bool Active { get; set; } = true;
        public LevelTracker LevelTracker { get; private set; }

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

            var items = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText("D:\\GitHub\\WowPixelBot\\items.json"));
            var creatures = JsonConvert.DeserializeObject<List<Creature>>(File.ReadAllText("D:\\GitHub\\WowPixelBot\\creatures.json"));

            this.BagReader = new BagReader(squareReader, 20, items);
            this.equipmentReader = new EquipmentReader(squareReader, 30);
            this.PlayerReader = new PlayerReader(squareReader, logger, this.BagReader, creatures);
            this.LevelTracker = new LevelTracker(PlayerReader);
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
                GC.Collect();
            }
        }

        public Color GetColorAt(int index)
        {
            return FrameColor[index];
        }
    }
}