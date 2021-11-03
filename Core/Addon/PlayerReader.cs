using System;
using Core.Database;
using System.Collections.Generic;

namespace Core
{
    public partial class PlayerReader
    {
        private readonly ISquareReader reader;
        private readonly CreatureDB creatureDb;

        public bool Initialized = false;

        public PlayerReader(ISquareReader reader, CreatureDB creatureDb)
        {
            this.reader = reader;
            this.creatureDb = creatureDb;
        }

        public Dictionary<Form, int> FormCost { private set; get; } = new Dictionary<Form, int>();

        public WowPoint PlayerLocation => new WowPoint(XCoord, YCoord, ZCoord);

        public double XCoord => reader.GetFixedPointAtCell(1) * 10;
        public double YCoord => reader.GetFixedPointAtCell(2) * 10;
        public double ZCoord { get; set; }
        public double Direction => reader.GetFixedPointAtCell(3);

        public RecordInt UIMapId = new RecordInt(4);

        public int Level => reader.GetIntAtCell(5);

        public WowPoint CorpseLocation => new WowPoint(CorpseX, CorpseY);
        public double CorpseX => reader.GetFixedPointAtCell(6) * 10;
        public double CorpseY => reader.GetFixedPointAtCell(7) * 10;

        public PlayerBitValues Bits => new PlayerBitValues(reader.GetIntAtCell(8), reader.GetIntAtCell(9));

        public int HealthMax => reader.GetIntAtCell(10);
        public int HealthCurrent => reader.GetIntAtCell(11);
        public int HealthPercent => HealthMax == 0 || HealthCurrent == 1 ? 0 : (HealthCurrent * 100) / HealthMax;

        public int PTMax => reader.GetIntAtCell(12); // Maximum amount of Power Type (dynamic)
        public int PTCurrent => reader.GetIntAtCell(13); // Current amount of Power Type (dynamic)
        public int PTPercentage => PTMax == 0 ? 0 : (PTCurrent * 100) / PTMax; // Power Type (dynamic) in terms of a percentage

        public int ManaMax => reader.GetIntAtCell(14);
        public int ManaCurrent => reader.GetIntAtCell(15);
        public int ManaPercentage => ManaMax == 0 ? 0 : (ManaCurrent * 100) / ManaMax;


        public bool HasTarget => Bits.HasTarget;// || TargetHealth > 0;
        public string Target
        {
            get
            {
                if (Bits.HasTarget && creatureDb.Entries.TryGetValue(TargetId, out var creature))
                {
                    return creature.Name;
                }
                return reader.GetStringAtCell(16) + reader.GetStringAtCell(17);
            }
        }

        public int TargetMaxHealth => reader.GetIntAtCell(18);
        public int TargetHealth => reader.GetIntAtCell(19);
        public int TargetHealthPercentage => TargetMaxHealth == 0 || TargetHealth == 1 ? 0 : (TargetHealth * 100) / TargetMaxHealth;


        public int PetMaxHealth => reader.GetIntAtCell(38);
        public int PetHealth => reader.GetIntAtCell(39);
        public int PetHealthPercentage => PetMaxHealth == 0 || PetHealth == 1 ? 0 : (PetHealth * 100) / PetMaxHealth;


        public SpellInRange SpellInRange => new SpellInRange(reader.GetIntAtCell(40));
        public bool WithInPullRange => SpellInRange.WithinPullRange(this, Class);
        public bool WithInCombatRange => SpellInRange.WithinCombatRange(this, Class);

        public BuffStatus Buffs => new BuffStatus(reader.GetIntAtCell(41));
        public TargetDebuffStatus TargetDebuffs => new TargetDebuffStatus(reader.GetIntAtCell(42));

        public int TargetLevel => reader.GetIntAtCell(43);

        public int Gold => reader.GetIntAtCell(44) + (reader.GetIntAtCell(45) * 1000000);

        public RaceEnum Race => (RaceEnum)(reader.GetIntAtCell(46) / 100f);

        public PlayerClassEnum Class => (PlayerClassEnum)(reader.GetIntAtCell(46) - ((int)Race * 100f));

        public bool Unskinnable => reader.GetIntAtCell(47) != 0; // Returns 1 if creature is unskinnable

        public Stance Stance => new Stance(reader.GetIntAtCell(48));
        public Form Form => Stance.Get(this, Class);

        public int MinRange => (int)(reader.GetIntAtCell(49) / 100000f);
        public int MaxRange => (int)((reader.GetIntAtCell(49) - (MinRange * 100000f)) / 100f);

        public bool IsInMeleeRange => MinRange == 0 && MaxRange != 0 && MaxRange <= 5;
        public bool IsInDeadZone => MinRange >= 5 && Bits.IsInDeadZoneRange; // between 5-8 yard - hunter and warrior

        public int PlayerXp => reader.GetIntAtCell(50);
        public int PlayerMaxXp => reader.GetIntAtCell(51);
        public int PlayerXpPercentage => (PlayerXp * 100) / (PlayerMaxXp == 0 ? 1 : PlayerMaxXp);

        private int UIErrorMessage => reader.GetIntAtCell(52);
        public UI_ERROR LastUIErrorMessage { get; set; }

        public int SpellBeingCast => reader.GetIntAtCell(53);
        public bool IsCasting => SpellBeingCast != 0;

        public int ComboPoints => reader.GetIntAtCell(54);

        public AuraCount AuraCount => new AuraCount(reader, 55);

        public int TargetId => reader.GetIntAtCell(56);
        public int TargetGuid => reader.GetIntAtCell(57);

        public int SpellBeingCastByTarget => reader.GetIntAtCell(58);
        public bool IsTargetCasting => SpellBeingCastByTarget != 0;

        public TargetTargetEnum TargetTarget => (TargetTargetEnum)reader.GetIntAtCell(59);

        public RecordInt AutoShot { private set; get; } = new RecordInt(60);
        public RecordInt MainHandSwing { private set; get; } = new RecordInt(61);
        public RecordInt CastEvent { private set; get; } = new RecordInt(62);
        public RecordInt CastSpellId { private set; get; } = new RecordInt(63);

        public int PetGuid => reader.GetIntAtCell(68);
        public int PetTargetGuid => reader.GetIntAtCell(69);
        public bool PetHasTarget => PetTargetGuid != 0;

        public int CastCount => reader.GetIntAtCell(70);

        public BitStatus CustomTrigger1 => new BitStatus(reader.GetIntAtCell(73));

        public int LastLootTime => reader.GetIntAtCell(97);

        public RecordInt GlobalTime { private set; get; } = new RecordInt(98);

        // https://wowpedia.fandom.com/wiki/Mob_experience
        public bool TargetYieldXP => Level switch
        {
            int n when n < 5 => true,
            int n when n >= 6 && n <= 39 => TargetLevel > (Level - Math.Floor(Level / 10f) - 5),
            int n when n >= 40 && n <= 59 => TargetLevel > (Level - Math.Floor(Level / 5f) - 5),
            int n when n >= 60 && n <= 70 => TargetLevel > Level - 9,
            _ => false
        };

        internal void Updated()
        {
            if (GlobalTime.Updated(reader) && (GlobalTime.Value <= 3 || !Initialized))
            {
                Reset();
            }

            if (UIErrorMessage > 0)
            {
                LastUIErrorMessage = (UI_ERROR)UIErrorMessage;
            }

            UIMapId.Update(reader);

            AutoShot.Update(reader);
            MainHandSwing.Update(reader);
            CastEvent.Update(reader);
            CastSpellId.Update(reader);
        }

        internal void Reset()
        {
            FormCost.Clear();

            // Reset all RecordInt
            UIMapId.Reset();

            AutoShot.Reset();
            MainHandSwing.Reset();
            CastEvent.Reset();
            CastSpellId.Reset();

            Initialized = true;
        }
    }
}