using System;
using Core.Database;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

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



        public Dictionary<Form, int> FormCost { get; set; } = new Dictionary<Form, int>();

        public WowPoint PlayerLocation => new WowPoint(XCoord, YCoord, ZCoord);

        public double XCoord => reader.GetFixedPointAtCell(1) * 10;
        public double YCoord => reader.GetFixedPointAtCell(2) * 10;
        public double Direction => reader.GetFixedPointAtCell(3);

        public double ZCoord { get; set; }

        public RecordInt UIMapId = new RecordInt(4);

        public int PlayerLevel => reader.GetIntAtCell(5);

        public WowPoint CorpseLocation => new WowPoint(CorpseX, CorpseY);

        // gets the position of your corpse where you died
        public double CorpseX => reader.GetFixedPointAtCell(6) * 10;

        public double CorpseY => reader.GetFixedPointAtCell(7) * 10;

        public PlayerBitValues PlayerBitValues => new PlayerBitValues(reader.GetIntAtCell(8), reader.GetIntAtCell(9));

        public int HealthMax => reader.GetIntAtCell(10);

        public int HealthCurrent => reader.GetIntAtCell(11);
        public int HealthPercent => HealthMax == 0 || HealthCurrent == 1 ? 0 : (HealthCurrent * 100) / HealthMax;

        public int PTMax => reader.GetIntAtCell(12); // Maximum amount of Power Type (dynamic)
        public int PTCurrent => reader.GetIntAtCell(13); // Current amount of Power Type (dynamic)
        public int PTPercentage => PTMax == 0 ? 0 : (PTCurrent * 100) / PTMax; // Power Type (dynamic) in terms of a percentage


        public int ManaMax => reader.GetIntAtCell(14);
        public int ManaCurrent => reader.GetIntAtCell(15);
        public int ManaPercentage => ManaMax == 0 ? 0 : (ManaCurrent * 100) / ManaMax;

        public string Target
        {
            get
            {
                if (TargetId > 0 && creatureDb.Entries.ContainsKey(this.TargetId))
                {
                    return creatureDb.Entries[this.TargetId].Name;
                }
                return reader.GetStringAtCell(16) + (reader.GetStringAtCell(17));
            }
        }

        public int TargetMaxHealth => reader.GetIntAtCell(18);

        public int TargetHealthPercentage => TargetMaxHealth == 0 || TargetHealth == 1 ? 0 : (TargetHealth * 100) / TargetMaxHealth;

        public int TargetHealth => reader.GetIntAtCell(19);

        public bool HasTarget => PlayerBitValues.HasTarget || TargetHealth > 0;

        public ActionBarBits CurrentAction => new ActionBarBits(this, reader, 26, 27, 28, 29, 30);
        public ActionBarBits UsableAction => new ActionBarBits(this, reader, 31, 32, 33, 34, 35);

        // 36 Actionbar cost

        // 37 unused

        public int PetMaxHealth => reader.GetIntAtCell(38);
        public int PetHealth => reader.GetIntAtCell(39);

        public int PetHealthPercentage => PetMaxHealth == 0 || PetHealth == 1 ? 0 : (PetHealth * 100) / PetMaxHealth;

        public SpellInRange SpellInRange => new SpellInRange(reader.GetIntAtCell(40));

        public bool WithInPullRange => SpellInRange.WithinPullRange(this, PlayerClass);
        public bool WithInCombatRange => SpellInRange.WithinCombatRange(this, PlayerClass);


        public BuffStatus Buffs => new BuffStatus(reader.GetIntAtCell(41));
        public DebuffStatus Debuffs => new DebuffStatus(reader.GetIntAtCell(42));

        public int TargetLevel => reader.GetIntAtCell(43);

        public int Gold => reader.GetIntAtCell(44) + (reader.GetIntAtCell(45) * 1000000);

        public RaceEnum PlayerRace => (RaceEnum)(reader.GetIntAtCell(46) / 100f);

        public PlayerClassEnum PlayerClass => (PlayerClassEnum)(reader.GetIntAtCell(46) - ((int)PlayerRace * 100f));

        public bool Unskinnable => reader.GetIntAtCell(47) != 0; // Returns 1 if creature is unskinnable

        public Stance Stance => new Stance(reader.GetIntAtCell(48));
        public Form Form => Stance.Get(this, PlayerClass);

        public int MinRange => (int)(reader.GetIntAtCell(49) / 100000f);
        public int MaxRange => (int)((reader.GetIntAtCell(49) - (MinRange * 100000f)) / 100f);

        public bool IsInMeleeRange => MinRange == 0 && (PlayerClass == PlayerClassEnum.Druid && PlayerLevel >= 10 ? MaxRange == 2 : MaxRange == 5);
        public bool IsInDeadZone => MinRange >= 5 && PlayerBitValues.IsInDeadZoneRange;

        public int PlayerXp => reader.GetIntAtCell(50);
        public int PlayerMaxXp => reader.GetIntAtCell(51);
        public int PlayerXpPercentage => (PlayerXp * 100) / (PlayerMaxXp == 0 ? 1 : PlayerMaxXp);

        private int UIErrorMessage => reader.GetIntAtCell(52);
        public UI_ERROR LastUIErrorMessage { get; set; }


        public bool IsAutoAttacking => PlayerBitValues.IsAutoRepeatSpellOn_AutoAttack;
        public bool IsShooting => PlayerBitValues.IsAutoRepeatSpellOn_Shoot;

        public bool IsAutoShoting => PlayerBitValues.IsAutoRepeatSpellOn_AutoShot;

        public int SpellBeingCast => reader.GetIntAtCell(53);
        public int ComboPoints => reader.GetIntAtCell(54);

        public AuraCount AuraCount => new AuraCount(reader, 55);

        public int PlayerDebuffCount => AuraCount.PlayerDebuff;
        public int PlayerBuffCount => AuraCount.PlayerBuff;

        public int TargetBuffCount => AuraCount.TargetBuff;
        public int TargetDebuffCount => AuraCount.TargetDebuff;


        public int TargetId => reader.GetIntAtCell(56);
        public int TargetGuid => reader.GetIntAtCell(57);

        public bool IsCasting => SpellBeingCast != 0;

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
        public bool TargetYieldXP => PlayerLevel switch
        {
            int n when n < 5 => true,
            int n when n >= 6 && n <= 39 => TargetLevel > (PlayerLevel - Math.Floor(PlayerLevel / 10f) - 5),
            int n when n >= 40 && n <= 59 => TargetLevel > (PlayerLevel - Math.Floor(PlayerLevel / 5f) - 5),
            int n when n >= 60 && n <= 70 => TargetLevel > PlayerLevel - 9,
            _ => false
        };



        #region Last Combat Kill Count

        private int lastCombatKillCount;
        public int LastCombatKillCount => lastCombatKillCount;

        public void IncrementKillCount()
        {
            lastCombatKillCount++;
        }

        public void DecrementKillCount()
        {
            lastCombatKillCount--;
            if (lastCombatKillCount < 0)
            {
                ResetKillCount();
            }
        }

        public void ResetKillCount()
        {
            lastCombatKillCount = 0;
        }

        #endregion


        #region Corpse Consumption

        public bool NeedLoot { get; set; } = false;
        public bool NeedSkin { get; set; } = false;

        private bool shouldConsumeCorpse;
        public bool ShouldConsumeCorpse => shouldConsumeCorpse;

        public void ProduceCorpse()
        {
            shouldConsumeCorpse = true;
        }

        public void ConsumeCorpse()
        {
            shouldConsumeCorpse = false;
        }

        #endregion


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