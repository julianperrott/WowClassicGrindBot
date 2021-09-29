using System;
using Core.Database;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public partial class PlayerReader
    {
        private readonly ISquareReader reader;
        private readonly CreatureDB creatureDb;

        public PlayerReader(ISquareReader reader, CreatureDB creatureDb)
        {
            this.reader = reader;
            this.creatureDb = creatureDb;
        }

        public double AvgUpdateLatency = 5;

        public int Sequence { get; private set; } = 0;

        public List<CreatureHistory> Creatures { get; } = new List<CreatureHistory>();
        public List<CreatureHistory> Targets { get; } = new List<CreatureHistory>();
        public List<CreatureHistory> DamageDone { get; } = new List<CreatureHistory>();
        public List<CreatureHistory> DamageTaken { get; } = new List<CreatureHistory>();
        public List<CreatureHistory> Deads { get; } = new List<CreatureHistory>();

        public WowPoint PlayerLocation => new WowPoint(XCoord, YCoord);

        public double XCoord => reader.GetFixedPointAtCell(1) * 10;
        public double YCoord => reader.GetFixedPointAtCell(2) * 10;
        public double Direction => reader.GetFixedPointAtCell(3);

        public int UIMapId => (int)reader.GetLongAtCell(4);

        public long PlayerLevel => reader.GetLongAtCell(5);

        public WowPoint CorpseLocation => new WowPoint(CorpseX, CorpseY);

        // gets the position of your corpse where you died
        public double CorpseX => reader.GetFixedPointAtCell(6) * 10;

        public double CorpseY => reader.GetFixedPointAtCell(7) * 10;

        public PlayerBitValues PlayerBitValues => new PlayerBitValues(reader.GetLongAtCell(8));

        // Player health and mana
        public long HealthMax => reader.GetLongAtCell(10); // Maximum amount of health of player

        public long HealthCurrent => reader.GetLongAtCell(11); // Current amount of health of player
        public long HealthPercent => HealthMax == 0 || HealthCurrent == 1 ? 0 : (HealthCurrent * 100) / HealthMax; // Health in terms of a percentage

        public long ManaMax => reader.GetLongAtCell(12); // Maximum amount of mana
        public long ManaCurrent => reader.GetLongAtCell(13); // Current amount of mana
        public long ManaPercentage => ManaMax == 0 ? 0 : (ManaCurrent * 100) / ManaMax; // Mana in terms of a percentage

        public bool IsInMeleeRange => MinRange == 0 && MaxRange == 5;
        public bool IsInDeadZone => MinRange >= 5 && PlayerBitValues.IsInDeadZoneRange;

        public long MinRange => (long)(reader.GetLongAtCell(15) / 100000f);
        public long MaxRange => (long)((reader.GetLongAtCell(15)-(MinRange*100000f)) / 100f);

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

        public long TargetMaxHealth => reader.GetLongAtCell(18);

        // Targets current percentage of health
        public long TargetHealthPercentage => TargetMaxHealth == 0 || TargetHealth == 1 ? 0 : (TargetHealth * 100) / TargetMaxHealth;

        public long TargetHealth => reader.GetLongAtCell(19);

        public bool HasTarget => PlayerBitValues.HasTarget || TargetHealth > 0;

        public ActionBarBits CurrentAction => new ActionBarBits(reader, 26, 27, 28, 29, 30);
        public ActionBarBits UsableAction => new ActionBarBits(reader, 31, 32, 33, 34, 35);

        // 36 Actionbar cost

        // 37 unused

        public long PetMaxHealth => reader.GetLongAtCell(38);
        public long PetHealth => reader.GetLongAtCell(39);

        public long PetHealthPercentage => PetMaxHealth == 0 || PetHealth == 1 ? 0 : (PetHealth * 100) / PetMaxHealth;

        public SpellInRange SpellInRange => new SpellInRange(reader.GetLongAtCell(40));

        public bool WithInPullRange => SpellInRange.WithinPullRange(this, PlayerClass);
        public bool WithInCombatRange => SpellInRange.WithinCombatRange(this, PlayerClass);


        public BuffStatus Buffs => new BuffStatus(reader.GetLongAtCell(41));
        public DebuffStatus Debuffs => new DebuffStatus(reader.GetLongAtCell(42));

        public long TargetLevel => reader.GetLongAtCell(43);

        public long Gold => reader.GetLongAtCell(44) + (reader.GetLongAtCell(45) * 1000000);

        public PlayerClassEnum PlayerClass => (PlayerClassEnum)reader.GetLongAtCell(46);

        public bool Unskinnable => reader.GetLongAtCell(47) != 0; // Returns 1 if creature is unskinnable

        public ShapeshiftForm Druid_ShapeshiftForm => (ShapeshiftForm)reader.GetLongAtCell(48);

        public Stance Stance => new Stance(reader.GetLongAtCell(48));

        public Form Form => Stance.Get(PlayerClass);

        public long PlayerXp => reader.GetLongAtCell(50);
        public long PlayerMaxXp => reader.GetLongAtCell(51);
        public long PlayerXpPercentage => (PlayerXp * 100) / (PlayerMaxXp == 0 ? 1 : PlayerMaxXp);

        private long UIErrorMessage => reader.GetLongAtCell(52);
        public UI_ERROR LastUIErrorMessage { get; set; }


        public bool IsAutoAttacking => PlayerBitValues.IsAutoRepeatSpellOn_AutoAttack;
        public bool IsShooting => PlayerBitValues.IsAutoRepeatSpellOn_Shoot;

        public bool IsAutoShoting => PlayerBitValues.IsAutoRepeatSpellOn_AutoShot;

        public long SpellBeingCast => reader.GetLongAtCell(53);
        public long ComboPoints => reader.GetLongAtCell(54);

        public int TargetId => (int)reader.GetLongAtCell(56);
        public long TargetGuid => reader.GetLongAtCell(57);

        public bool IsCasting => SpellBeingCast != 0;

        public TargetTargetEnum TargetTarget => (TargetTargetEnum)reader.GetLongAtCell(59);


        public int LastCombatCreatureGuid => (int)reader.GetLongAtCell(64);

        public int LastCombatDamageDoneGuid => (int)reader.GetLongAtCell(65);

        public int LastCombatDamageTakenGuid => (int)reader.GetLongAtCell(66);

        public int LastDeadGuid => (int)reader.GetLongAtCell(67);


        public int PetGuid => (int)reader.GetLongAtCell(68);
        public int PetTargetGuid => (int)reader.GetLongAtCell(69);
        public bool PetHasTarget => PetTargetGuid != 0;

        public long GlobalTime => reader.GetLongAtCell(70);
        public long LastLootTime => reader.GetLongAtCell(71);

        // https://wowpedia.fandom.com/wiki/Mob_experience
        public bool TargetYieldXP => PlayerLevel switch
        {
            long n when n < 5 => true,
            long n when n >= 6 && n <= 39 => TargetLevel > (PlayerLevel - Math.Floor(PlayerLevel / 10f) - 5),
            long n when n >= 40 && n <= 59 => TargetLevel > (PlayerLevel - Math.Floor(PlayerLevel / 5f) - 5),
            long n when n >= 60 && n <= 70 => TargetLevel > PlayerLevel - 9,
            _ => false
        };


        #region Combat Creatures
        public int CombatCreatureCount => Creatures.Count;

        public void UpdateCreatureLists()
        {
            CreatureHistory.Update(LastCombatCreatureGuid, 100f, Creatures);
            CreatureHistory.Update(LastCombatDamageTakenGuid, 100f, DamageTaken);
            CreatureHistory.Update(LastCombatDamageDoneGuid, (int)TargetHealthPercentage, DamageDone);

            // set dead mob health everywhere
            CreatureHistory.Update(LastDeadGuid, 0, Deads);
            CreatureHistory.Update(LastDeadGuid, 0, Creatures);
            CreatureHistory.Update(LastDeadGuid, 0, DamageTaken);
            CreatureHistory.Update(LastDeadGuid, 0, DamageDone);

            CreatureHistory.Update((int)TargetGuid, (int)TargetHealthPercentage, Targets);

            // Update last target health from LastDeadGuid
            if (Targets.FindIndex(x => x.CreatureId == LastDeadGuid) != -1)
            {
                CreatureHistory.Update(LastDeadGuid, 0, Targets);
            }
        }

        #endregion


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
            if(lastCombatKillCount < 0)
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
            Sequence++;

            if (UIErrorMessage > 0)
            {
                LastUIErrorMessage = (UI_ERROR)UIErrorMessage;
            }
        }
    }
}