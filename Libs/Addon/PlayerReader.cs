
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Libs
{
    public partial class PlayerReader
    {
        private readonly ISquareReader reader;
        private readonly BagReader bagReader;
        private ILogger logger;

        public PlayerReader(ISquareReader reader, ILogger logger, BagReader bagReader)
        {
            this.reader = reader;
            this.logger = logger;
            this.bagReader = bagReader;
        }

        public int Sequence { get; private set; } = 0;

        public WowPoint PlayerLocation => new WowPoint(XCoord, YCoord);

        public double XCoord => reader.GetFixedPointAtCell(1) * 100;
        public double YCoord => reader.GetFixedPointAtCell(2) * 100;
        public double Direction => reader.GetFixedPointAtCell(3);

        public string Zone => reader.GetStringAtCell(4) + reader.GetStringAtCell(5); // Checks current geographic zone

        public WowPoint CorpseLocation => new WowPoint(CorpseX, CorpseY);

        // gets the position of your corpse where you died
        public double CorpseX => reader.GetFixedPointAtCell(6) * 10;

        public double CorpseY => reader.GetFixedPointAtCell(7) * 10;

        public PlayerBitValues PlayerBitValues => new PlayerBitValues(reader.GetLongAtCell(8));

        // Player health and mana
        public long HealthMax => reader.GetLongAtCell(10); // Maximum amount of health of player

        internal void Updated()
        {
            Sequence++;
            //logger.LogInformation($"Target is me = {PlayerBitValues.TargetOfTargetIsPlayer}");
            //logger.LogInformation($"{SpellInRange.Rogue_SinisterStrike}-{SpellInRange.Rogue_Throw}-{SpellInRange.Rogue_ShootGun}");

            if (UIErrorMessage > 0)
            {
                LastUIErrorMessage = (UI_ERROR)UIErrorMessage;
            }
        }

        public long HealthCurrent => reader.GetLongAtCell(11); // Current amount of health of player
        public long HealthPercent => HealthMax == 0 || HealthCurrent == 1 ? 0 : (HealthCurrent * 100) / HealthMax; // Health in terms of a percentage

        public long ManaMax => reader.GetLongAtCell(12); // Maximum amount of mana
        public long ManaCurrent => reader.GetLongAtCell(13); // Current amount of mana
        public long ManaPercentage => ManaMax == 0 ? 0 : (ManaCurrent * 100) / ManaMax; // Mana in terms of a percentage

        public long PlayerLevel => reader.GetLongAtCell(14); // Level is our character's exact level ranging from 1-60

        // Todo !
        // range detects if a target range. Bases information off of action slot 2, 3, and 4. Outputs: 50, 35, 30, or 20
        public long Range => reader.GetLongAtCell(15);

        // target bane
        public string Target => reader.GetStringAtCell(16) + (reader.GetStringAtCell(17));

        public long TargetMaxHealth => reader.GetLongAtCell(18);

        // Targets current percentage of health
        public double TargetHealthPercentage => ((double)TargetHealth * 100) / TargetMaxHealth;

        public long TargetHealth => reader.GetLongAtCell(19);

        public bool HasTarget => !string.IsNullOrEmpty(Target) || TargetHealth > 0;

        internal async Task WaitForUpdate()
        {
            var s = this.Sequence;
            while (this.Sequence == s)
            {
                await Task.Delay(100);
            }
        }

        // 32 - 33
        public long Gold => reader.GetLongAtCell(32) + reader.GetLongAtCell(33) * 1000000;

        // 34 -36 Loops through binaries of three pixels. Currently does 24 slots. 1-12 and 61-72.
        public ActionBarStatus ActionBarUseable_1To24 => new ActionBarStatus(reader.GetLongAtCell(34));

        public ActionBarStatus ActionBarUseable_25To48 => new ActionBarStatus(reader.GetLongAtCell(35));
        public ActionBarStatus ActionBarUseable_49To72 => new ActionBarStatus(reader.GetLongAtCell(36));
        public ActionBarStatus ActionBarUseable_73To96 => new ActionBarStatus(reader.GetLongAtCell(42));

        // 37- 40 Bag Slots
        public long BagSlot1Slots => reader.GetLongAtCell(37);

        public long BagSlot2Slots => reader.GetLongAtCell(38);
        public long BagSlot3Slots => reader.GetLongAtCell(39);
        public long BagSlot4Slots => reader.GetLongAtCell(40);

        public BuffStatus Buffs => new BuffStatus(reader.GetLongAtCell(41));
        public DebuffStatus Debuffs => new DebuffStatus(reader.GetLongAtCell(55));

        //public long SkinningLevel => reader.GetLongAtCell(41);
        //public long FishingLevel => reader.GetLongAtCell(42);

        //MakePixelSquareArr(integerToColor(self: GetDebuffs("FrostNova")), 43)-- Checks if target is frozen by frost nova debuff

        public long TargetLevel => reader.GetLongAtCell(43);

        //public long Gametime => reader.GetLongAtCell(44);// Returns time in the game
        //public long GossipOptions => reader.GetLongAtCell(45); //  Returns which gossip icons are on display in dialogue box

        public PlayerClassEnum PlayerClass => (PlayerClassEnum)reader.GetLongAtCell(46);

        public bool Unskinnable => reader.GetLongAtCell(47) != 0; // Returns 1 if creature is unskinnable
        //public long ShapeshiftForm => reader.GetLongAtCell(48);

        public ShapeshiftForm Druid_ShapeshiftForm => (ShapeshiftForm)reader.GetLongAtCell(48);

        public long PlayerXp => reader.GetLongAtCell(50);
        public long PlayerMaxXp => reader.GetLongAtCell(51);
        public long PlayerXpPercentage => (PlayerXp * 100) / (PlayerMaxXp == 0 ? 1 : PlayerMaxXp);

        private long UIErrorMessage => reader.GetLongAtCell(52);
        public UI_ERROR LastUIErrorMessage { get; set; }

        private SpellInRange spellInRange = new SpellInRange(0);

        public SpellInRange SpellInRange
        {
            get
            {
                var x = reader.GetLongAtCell(49);
                if (x < 1024) // ignore odd values
                {
                    spellInRange = new SpellInRange(x);
                }
                return spellInRange;
            }
        }

        public bool WithInPullRange => SpellInRange.WithInPullRange(this.PlayerClass);
        public bool WithInCombatRange => SpellInRange.WithInCombatRange(this.PlayerClass);

        public bool IsShooting => PlayerBitValues.IsAutoRepeatActionOnActionBar10;
        public bool IsActionBar10Current => PlayerBitValues.IsCurrentActionOnActionBar10;


        public long SpellBeingCast => reader.GetLongAtCell(53);
        public long ComboPoints => reader.GetLongAtCell(54);

        public bool IsCasting => SpellBeingCast != 0;

        private Dictionary<string, Func<bool>> BuffDictionary = new Dictionary<string, Func<bool>>();


        public class Requirement
        {
            public Func<bool> HasRequirement { get; set; } = () => false;
            public Func<string> LogMessage { get; set; } = () => "Unknown requirement";
        }

        public Requirement GetRequirement(KeyConfiguration item)
        {
            if (item.Requirement.StartsWith("Health>"))
            {
                var minHealth = int.Parse(item.Requirement.Replace("Health>", "").Replace("%", ""));
                return new Requirement
                {
                    HasRequirement = () => this.HealthPercent >= minHealth,
                    LogMessage = () => $"health too high: {HealthPercent} > {minHealth}"
                };
            }
            else if (item.Requirement.StartsWith("Mana>"))
            {
                var minMana = int.Parse(item.Requirement.Replace("Mana>", "").Replace("%", ""));
                return new Requirement
                {
                    HasRequirement = () => this.ManaPercentage >= minMana || this.Druid_ShapeshiftForm > ShapeshiftForm.None,
                    LogMessage = () => $"mana too high: {ManaPercentage} > {minMana}"
                };
            }
            else if (item.Requirement.StartsWith("BagItem:"))
            {
                var itemId = int.Parse(item.Requirement.Replace("BagItem:", ""));
                return new Requirement
                {
                    HasRequirement = () => this.bagReader.Contains(itemId),
                    LogMessage = () => $"item {itemId} not found in bag"
                };
            }

            if (BuffDictionary.Count == 0)
            {
                BuffDictionary = new Dictionary<string, Func<bool>>
                {
                    {  "Seal", ()=> this.Buffs.Seal },
                    {  "Aura", ()=>Buffs.Aura },
                    {  "Devotion Aura", ()=>Buffs.Aura },
                    {  "Blessing", ()=> Buffs.Blessing },
                    {  "Blessing of Might", ()=> Buffs.Blessing },
                    {  "Well Fed", ()=> Buffs.WellFed },
                    {  "Eating", ()=> Buffs.Eating },
                    {  "Drinking", ()=> Buffs.Drinking },
                    {  "Mana Regeneration", ()=> Buffs.ManaRegeneration },
                    {  "Fortitude", ()=> Buffs.Fortitude },
                    {  "InnerFire", ()=> Buffs.InnerFire },
                    {  "Renew", ()=> Buffs.Renew },
                    {  "Shield", ()=> Buffs.Shield },
                    {  "Mark of the Wild", ()=> Buffs.MarkOfTheWild },
                    {  "Thorns", ()=> Buffs.Thorns },
                    {  "Frost Armor", ()=> Buffs.FrostArmor },
                    {  "Arcane Intellect", ()=> Buffs.ArcaneIntellect },
                    {  "Ice Barrier", ()=> Buffs.IceBarrier },
                    {  "Slice And Dice", ()=> Buffs.SliceAndDice },
                    {  "Battle Shout", ()=> Buffs.BattleShout },

                    {  "Demoralizing Roar", ()=> Debuffs.Roar },
                    {  "Faerie Fire", ()=> Debuffs.FaerieFire },

                    { "OutOfCombatRange", ()=> this.WithInCombatRange },
                    { "InCombatRange", ()=> !this.WithInCombatRange }
                };
            }

            if (BuffDictionary.Keys.Contains(item.Requirement))
            {
                return new Requirement
                {
                    HasRequirement = BuffDictionary[item.Requirement],
                    LogMessage = () => $"Buff {item.Requirement} missing"
                };
            }
            else
            {
                logger.LogInformation($"UNKNOWN BUFF! {item.Name} - {item.Requirement}: try one of: {string.Join(", ", BuffDictionary.Keys)}");
                return new Requirement
                {
                    HasRequirement = () => true,
                    LogMessage = () => $"UNKNOWN BUFF! {item.Requirement}"
                };
            }
        }
    }
}