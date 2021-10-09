using Core.Database;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using Cyotek.Collections;
using Cyotek.Collections.Generic;

namespace Core
{
    public sealed class AddonReader : IAddonReader, IDisposable
    {
        private readonly ILogger logger;
        private readonly ISquareReader squareReader;
        private readonly IAddonDataProvider addonDataProvider;
        public bool Active { get; set; } = true;
        public PlayerReader PlayerReader { get; set; }
        public BagReader BagReader { get; set; }
        public EquipmentReader equipmentReader { get; set; }

        public ActionBarCostReader ActionBarCostReader { get; set; }

        public GossipReader GossipReader { get; set; }

        public LevelTracker LevelTracker { get; set; }

        public event EventHandler? AddonDataChanged;
        public event EventHandler? ZoneChanged;

        private readonly AreaDB areaDb;
        public WorldMapAreaDB WorldMapAreaDb { get; set; }
        private readonly ItemDB itemDb;
        private readonly CreatureDB creatureDb;

        private readonly CircularBuffer<double> UpdateLatencys;

        private DateTime lastFrontendUpdate = DateTime.Now;
        private readonly int FrontendUpdateIntervalMs = 250;

        public AddonReader(ILogger logger, DataConfig dataConfig, AreaDB areaDb, IAddonDataProvider addonDataProvider)
        {
            this.logger = logger;
            this.addonDataProvider = addonDataProvider;

            this.squareReader = new SquareReader(this);

            this.itemDb = new ItemDB(logger, dataConfig);
            this.creatureDb = new CreatureDB(logger, dataConfig);

            this.equipmentReader = new EquipmentReader(squareReader, 24, 25);
            this.BagReader = new BagReader(squareReader, itemDb, equipmentReader, 20, 21, 22, 23);

            this.ActionBarCostReader = new ActionBarCostReader(squareReader, 36);

            this.GossipReader = new GossipReader(squareReader, 37);

            this.PlayerReader = new PlayerReader(squareReader, creatureDb);
            this.LevelTracker = new LevelTracker(PlayerReader);

            this.areaDb = areaDb;
            this.WorldMapAreaDb = new WorldMapAreaDB(logger, dataConfig);

            UpdateLatencys = new CircularBuffer<double>(10);

            PlayerReader.UIMapId.Changed += (object obj, EventArgs e) => ZoneChanged?.Invoke(this, EventArgs.Empty);

            PlayerReader.GlobalTime.Changed += (object obj, EventArgs e) =>
            {
                UpdateLatencys.Put((DateTime.Now - PlayerReader.GlobalTime.LastChanged).TotalMilliseconds);
                PlayerReader.AvgUpdateLatency = 0;
                for (int i = 0; i < UpdateLatencys.Size; i++)
                {
                    PlayerReader.AvgUpdateLatency += UpdateLatencys.PeekAt(i);
                }
                PlayerReader.AvgUpdateLatency /= UpdateLatencys.Size;
            };
        }

        public void AddonRefresh()
        {
            Refresh();

            BagReader.Read();
            equipmentReader.Read();

            ActionBarCostReader.Read();

            GossipReader.Read();

            LevelTracker.Update();

            areaDb.Update(WorldMapAreaDb.GetAreaId(PlayerReader.UIMapId.Value));

            if ((DateTime.Now - lastFrontendUpdate).TotalMilliseconds >= FrontendUpdateIntervalMs)
            {
                AddonDataChanged?.Invoke(this, EventArgs.Empty);
                lastFrontendUpdate = DateTime.Now;
            }
        }

        public void Refresh()
        {
            addonDataProvider.Update();
            PlayerReader.Updated();
        }

        public void Reset()
        {
            PlayerReader.Initialized = false;
            PlayerReader.Reset();
        }

        public Color GetColorAt(int index)
        {
            return addonDataProvider.GetColor(index);
        }

        public void Dispose()
        {
            addonDataProvider?.Dispose();
        }
    }
}