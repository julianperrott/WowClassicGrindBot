using Libs.Addon;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Libs
{
    public partial class PlayerReader
    {
        private readonly ISquareReader reader;
        private readonly BagReader bagReader;
        private ILogger logger;
        public Dictionary<int, Creature> creatureDictionary = new Dictionary<int, Creature>();

        public PlayerReader(ISquareReader reader, ILogger logger, BagReader bagReader, List<Creature> creatures)
        {
            this.reader = reader;
            this.logger = logger;
            this.bagReader = bagReader;
            creatures.ForEach(i => creatureDictionary.Add(i.Entry, i));
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

        public string Target
        {
            get
            {
                if (TargetId > 0 && creatureDictionary.ContainsKey(this.TargetId))
                {
                    return creatureDictionary[this.TargetId].Name;
                }
                return reader.GetStringAtCell(16) + (reader.GetStringAtCell(17));
            }
        }

        public long TargetMaxHealth => reader.GetLongAtCell(18);

        // Targets current percentage of health
        public long TargetHealthPercentage => TargetMaxHealth == 0 || TargetHealth == 1 ? 0 : (TargetHealth * 100) / TargetMaxHealth;

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

        public long TargetLevel => reader.GetLongAtCell(43);

        public PlayerClassEnum PlayerClass => (PlayerClassEnum)reader.GetLongAtCell(46);

        public bool Unskinnable => reader.GetLongAtCell(47) != 0; // Returns 1 if creature is unskinnable

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

        public bool IsShooting => PlayerBitValues.IsAutoRepeatSpellOn_Shoot;

        public long SpellBeingCast => reader.GetLongAtCell(53);
        public long ComboPoints => reader.GetLongAtCell(54);

        public int TargetId => (int)reader.GetLongAtCell(56);
        public long TargetGuid => reader.GetLongAtCell(57);

        public long ZoneId => reader.GetLongAtCell(58);

        public bool IsCasting => SpellBeingCast != 0;

        public TargetTargetEnum TargetTarget => (TargetTargetEnum)reader.GetLongAtCell(59);
    }
}