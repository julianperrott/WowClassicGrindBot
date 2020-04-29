using Libs.Addon;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Libs
{
    public class WowData
    {
        public List<DataFrame> frames { get; private set; } = new List<DataFrame>();
        public IAddonReader AddonReader { get; private set; }
        private readonly ISquareReader squareReader;
        public PlayerReader PlayerReader { get; private set; }
        public BagReader BagReader { get; private set; }
        public EquipmentReader equipmentReader { get; private set; }
        public bool Active { get; set; } = true;
        public LevelTracker LevelTracker { get; private set; }

        public event EventHandler? AddonDataChanged;

        public WowData(IColorReader colorReader, List<DataFrame> frames, ILogger logger)
        {
            this.frames = frames;

            var width = frames.Last().point.X + 1;
            var height = frames.Max(f => f.point.Y) + 1;
            this.AddonReader = new AddonReader(colorReader, frames, width, height, logger);

            this.squareReader = new SquareReader(AddonReader);

            //read item database
            var itemFilename = $"D:\\GitHub\\WowPixelBot\\items.json";
            var items = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText(itemFilename));

            this.BagReader = new BagReader(squareReader, 20, items);
            this.equipmentReader = new EquipmentReader(squareReader, 30);
            this.PlayerReader = new PlayerReader(squareReader, logger, this.BagReader);
            this.LevelTracker = new LevelTracker(PlayerReader);

            this.AddonReader.PlayerReader = this.PlayerReader;
        }

        private int seq = 0;

        public void AddonRefresh()
        {
            AddonReader.Refresh();

            // 20 - 29
            var bagItems = BagReader.Read();

            // 30 - 31
            var equipment = equipmentReader.Read();

            LevelTracker.Update();

            //logger.LogInformation($"X: {PlayerReader.XCoord.ToString("0.00")}, Y: {PlayerReader.YCoord.ToString("0.00")}, Direction: {PlayerReader.Direction.ToString("0.00")}, Zone: {PlayerReader.Zone}, Gold: {PlayerReader.Gold}");

            //logger.LogInformation($"Enabled: {PlayerReader.ActionBarEnabledAction.value}, NotEnoughMana: {PlayerReader.ActionBarNotEnoughMana.value}, NotOnCooldown: {PlayerReader.ActionBarNotOnCooldown.value}, Charge: {PlayerReader.SpellInRange.Charge}, Rend: {PlayerReader.SpellInRange.Rend}, Shoot gun: {PlayerReader.SpellInRange.ShootGun}");
            seq++;

            if (seq >= 10)
            {
                seq = 0;
                AddonDataChanged?.Invoke(AddonReader, new EventArgs());
            }
            System.Threading.Thread.Sleep(10);
        }
    }
}