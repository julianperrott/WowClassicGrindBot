using Core.Database;
using Microsoft.Extensions.Logging;
using System;
using Cyotek.Collections.Generic;
using System.Linq;

namespace Core
{
    public sealed class AddonReader : IAddonReader, IDisposable
    {
        private readonly ILogger logger;
        private readonly ISquareReader squareReader;
        private readonly IAddonDataProvider addonDataProvider;

        public bool Initialized { get; private set; }

        public bool Active { get; set; } = true;
        public PlayerReader PlayerReader { get; private set; }

        public CreatureHistory CreatureHistory { get; private set; }

        public BagReader BagReader { get; private set; }
        public EquipmentReader EquipmentReader { get; private set; }

        public ActionBarCostReader ActionBarCostReader { get; private set; }

        public ActionBarCooldownReader ActionBarCooldownReader { get; private set; }

        public ActionBarBits CurrentAction => new ActionBarBits(PlayerReader, squareReader, 26, 27, 28, 29, 30);
        public ActionBarBits UsableAction => new ActionBarBits(PlayerReader, squareReader, 31, 32, 33, 34, 35);

        public GossipReader GossipReader { get; private set; }

        public SpellBookReader SpellBookReader { get; private set; }
        public TalentReader TalentReader { get; private set; }

        public LevelTracker LevelTracker { get; private set; }

        public event EventHandler? AddonDataChanged;
        public event EventHandler? ZoneChanged;
        public event EventHandler? PlayerDeath;

        public WorldMapAreaDB WorldMapAreaDb { get; private set; }
        public ItemDB ItemDb { get; private set; }
        public CreatureDB CreatureDb { get; private set; }
        public AreaDB AreaDb { get; private set; }

        private readonly SpellDB spellDb;
        private readonly TalentDB talentDB;

        public RecordInt UIMapId { private set; get; } = new RecordInt(4);

        public RecordInt GlobalTime { private set; get; } = new RecordInt(98);

        public int CombatCreatureCount => CreatureHistory.DamageTaken.Count(c => c.HealthPercent > 0);

        public string TargetName
        {
            get
            {
                return CreatureDb.Entries.TryGetValue(PlayerReader.TargetId, out SharedLib.Creature creature)
                    ? creature.Name
                    : squareReader.GetStringAtCell(16) + squareReader.GetStringAtCell(17);
            }
        }

        public double AvgUpdateLatency { private set; get; } = 5;
        private readonly CircularBuffer<double> UpdateLatencys;

        private DateTime lastFrontendUpdate;
        private readonly int FrontendUpdateIntervalMs = 250;

        public AddonReader(ILogger logger, DataConfig dataConfig, IAddonDataProvider addonDataProvider)
        {
            this.logger = logger;
            this.addonDataProvider = addonDataProvider;

            this.squareReader = new SquareReader(this);

            this.AreaDb = new AreaDB(logger, dataConfig);
            this.WorldMapAreaDb = new WorldMapAreaDB(logger, dataConfig);
            this.ItemDb = new ItemDB(logger, dataConfig);
            this.CreatureDb = new CreatureDB(logger, dataConfig);
            this.spellDb = new SpellDB(logger, dataConfig);
            this.talentDB = new TalentDB(logger, dataConfig, spellDb);

            this.CreatureHistory = new CreatureHistory(squareReader, 64, 65, 66, 67);

            this.EquipmentReader = new EquipmentReader(squareReader, 24, 25);
            this.BagReader = new BagReader(squareReader, ItemDb, EquipmentReader, 20, 21, 22, 23);

            this.ActionBarCostReader = new ActionBarCostReader(squareReader, 36);
            this.ActionBarCooldownReader = new ActionBarCooldownReader(squareReader, 37);

            this.GossipReader = new GossipReader(squareReader, 73);

            this.SpellBookReader = new SpellBookReader(squareReader, 71, spellDb);

            this.PlayerReader = new PlayerReader(squareReader);
            this.LevelTracker = new LevelTracker(PlayerReader, PlayerDeath, CreatureHistory);
            this.TalentReader = new TalentReader(squareReader, 72, PlayerReader, talentDB);

            UpdateLatencys = new CircularBuffer<double>(10);

            UIMapId.Changed += (object? obj, EventArgs e) =>
            {
                this.AreaDb.Update(WorldMapAreaDb.GetAreaId(UIMapId.Value));
                ZoneChanged?.Invoke(this, EventArgs.Empty);
            };

            GlobalTime.Changed += (object? obj, EventArgs e) =>
            {
                UpdateLatencys.Put((DateTime.UtcNow - GlobalTime.LastChanged).TotalMilliseconds);
                AvgUpdateLatency = 0;
                for (int i = 0; i < UpdateLatencys.Size; i++)
                {
                    AvgUpdateLatency += UpdateLatencys.PeekAt(i);
                }
                AvgUpdateLatency /= UpdateLatencys.Size;
            };
        }

        public void AddonRefresh()
        {
            Refresh();

            CreatureHistory.Update(PlayerReader.TargetGuid, PlayerReader.TargetHealthPercentage);

            BagReader.Read();
            EquipmentReader.Read();

            ActionBarCostReader.Read();
            ActionBarCooldownReader.Read();

            GossipReader.Read();

            SpellBookReader.Read();
            TalentReader.Read();

            if ((DateTime.UtcNow - lastFrontendUpdate).TotalMilliseconds >= FrontendUpdateIntervalMs)
            {
                AddonDataChanged?.Invoke(this, EventArgs.Empty);
                lastFrontendUpdate = DateTime.UtcNow;
            }
        }

        public void Refresh()
        {
            addonDataProvider.Update();

            if (GlobalTime.Updated(squareReader) && (GlobalTime.Value <= 3 || !Initialized))
            {
                Reset();
            }

            PlayerReader.Updated();

            UIMapId.Update(squareReader);
        }

        public void SoftReset()
        {
            LevelTracker.Reset();
            CreatureHistory.Reset();
        }

        public void Reset()
        {
            Initialized = false;

            PlayerReader.Reset();

            UIMapId.Reset();

            ActionBarCostReader.Reset();
            ActionBarCooldownReader.Reset();
            SpellBookReader.Reset();
            TalentReader.Reset();

            SoftReset();

            Initialized = true;
        }

        public int GetIntAt(int index)
        {
            return addonDataProvider.GetInt(index);
        }

        public void Dispose()
        {
            addonDataProvider?.Dispose();
        }

        public void PlayerDied()
        {
            PlayerDeath?.Invoke(this, EventArgs.Empty);
        }
    }
}