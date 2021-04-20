using Libs.Database;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Libs
{
    public class AddonReader : IAddonReader
    {
        public List<DataFrame> frames { get; private set; } = new List<DataFrame>();
        private Color[] FrameColor { get; set; } = new Color[200];

        private readonly ILogger logger;
        private readonly ISquareReader squareReader;
        private readonly DataConfig dataConfig;
        public PlayerReader PlayerReader { get; set; }
        public BagReader BagReader { get; set; }
        public EquipmentReader equipmentReader { get; set; }
        public bool Active { get; set; } = true;
        public LevelTracker LevelTracker { get; set; }

        public event EventHandler? AddonDataChanged;

        private readonly int width;
        private readonly int height;
        private readonly IColorReader colorReader;

        private readonly AreaDB? areaDb;
        private readonly WorldMapAreaDB worldMapAreaDb;
        private readonly ItemDB itemDb;
        private readonly CreatureDB creatureDb;

        public AddonReader(DataConfig dataConfig, IColorReader colorReader, List<DataFrame> frames, ILogger logger, AreaDB? areaDb)
        {
            this.dataConfig = dataConfig;
            this.frames = frames;
            this.logger = logger;
            this.colorReader = colorReader;
            this.width = frames.Last().point.X + 1;
            this.height = frames.Max(f => f.point.Y) + 1;
            this.squareReader = new SquareReader(this);

            this.itemDb = new ItemDB(logger, dataConfig);
            this.creatureDb = new CreatureDB(logger, dataConfig);

            this.BagReader = new BagReader(squareReader, 20, itemDb);
            this.equipmentReader = new EquipmentReader(squareReader, 30);
            this.PlayerReader = new PlayerReader(squareReader, creatureDb);
            this.LevelTracker = new LevelTracker(PlayerReader);

            this.areaDb = areaDb;
            this.worldMapAreaDb = new WorldMapAreaDB(logger, dataConfig);
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

            this.PlayerReader.UpdateCreatureLists();

            areaDb?.Update(worldMapAreaDb.GetAreaId(PlayerReader.ZoneId));

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
                using (var bitMap = colorReader.GetBitmap(this.width, this.height))
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