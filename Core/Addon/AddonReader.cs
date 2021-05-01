using Core.Database;
using Microsoft.Extensions.Logging;
using SharedLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Core
{
    public sealed class AddonReader : IAddonReader, IDisposable
    {
        public List<DataFrame> frames { get; private set; } = new List<DataFrame>();
        private Color[] FrameColor { get; set; } = new Color[200];

        private readonly ILogger logger;
        private readonly ISquareReader squareReader;
        private readonly DataConfig dataConfig;
        private readonly WowScreen wowScreen;
        public PlayerReader PlayerReader { get; set; }
        public BagReader BagReader { get; set; }
        public EquipmentReader equipmentReader { get; set; }
        public bool Active { get; set; } = true;
        public LevelTracker LevelTracker { get; set; }

        public event EventHandler? AddonDataChanged;

        private readonly int width;
        private readonly int height;
        private readonly IColorReader colorReader;
        private readonly DirectBitmapCapturer capturer;

        private readonly AreaDB areaDb;
        private readonly WorldMapAreaDB worldMapAreaDb;
        private readonly ItemDB itemDb;
        private readonly CreatureDB creatureDb;

        public AddonReader(ILogger logger, DataConfig dataConfig, WowScreen wowScreen, List<DataFrame> frames, AreaDB areaDb)
        {
            this.logger = logger;
            this.dataConfig = dataConfig;
            this.wowScreen = wowScreen;
            this.frames = frames;

            this.width = frames.Last().point.X + 1;
            this.height = frames.Max(f => f.point.Y) + 1;

            wowScreen.GetRectangle(out var rect);
            rect.Width = width;
            rect.Height = height;
            capturer = new DirectBitmapCapturer(rect);
            colorReader = capturer;

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
            BagReader.Read();

            // 30 - 31
            equipmentReader.Read();

            LevelTracker.Update();

            PlayerReader.UpdateCreatureLists();

            areaDb.Update(worldMapAreaDb.GetAreaId(PlayerReader.ZoneId));

            seq++;

            if (seq >= 10)
            {
                seq = 0;
                AddonDataChanged?.Invoke(this, new EventArgs());
            }
        }

        public void Refresh()
        {
            try
            {
                wowScreen.GetPosition(out var p);
                capturer.Capture(new Rectangle(p.X, p.Y, width, height));

                frames.ForEach(frame => FrameColor[frame.index] = colorReader.GetColorAt(frame.point));

                PlayerReader.Updated();
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

        public void Dispose()
        {
            capturer?.Dispose();
        }
    }
}