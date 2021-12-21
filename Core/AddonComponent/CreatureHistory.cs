using System;
using System.Collections.Generic;

namespace Core
{
    public class CreatureHistory
    {
        private readonly ISquareReader reader;

        private const int LifeTimeInSeconds = 60;

        public event EventHandler? KillCredit;

        public List<CreatureRecord> Creatures { private set; get; } = new List<CreatureRecord>();
        public List<CreatureRecord> Targets { private set; get; } = new List<CreatureRecord>();
        public List<CreatureRecord> DamageDone { private set; get; } = new List<CreatureRecord>();
        public List<CreatureRecord> DamageTaken { private set; get; } = new List<CreatureRecord>();
        public List<CreatureRecord> Deads { private set; get; } = new List<CreatureRecord>();

        public RecordInt CombatCreatureGuid { private set; get; }
        public RecordInt CombatDamageDoneGuid { private set; get; }
        public RecordInt CombatDamageTakenGuid { private set; get; }
        public RecordInt CombatDeadGuid { private set; get; }

        public CreatureHistory(ISquareReader reader, int cCreature, int cDamageDone, int cDamageTaken, int cDead)
        {
            this.reader = reader;

            CombatCreatureGuid = new RecordInt(cCreature);
            CombatDamageDoneGuid = new RecordInt(cDamageDone);
            CombatDamageTakenGuid = new RecordInt(cDamageTaken);
            CombatDeadGuid = new RecordInt(cDead);
        }

        public void Reset()
        {
            Creatures.Clear();
            Targets.Clear();
            DamageDone.Clear();
            DamageTaken.Clear();
            Deads.Clear();

            CombatCreatureGuid.Reset();
            CombatDamageDoneGuid.Reset();
            CombatDamageTakenGuid.Reset();
            CombatDeadGuid.Reset();
        }

        public void Update(int targetGuid, int targetHealthPercent)
        {
            Update(targetGuid, targetHealthPercent, Targets);

            if (CombatCreatureGuid.Updated(reader))
            {
                Update(CombatCreatureGuid.Value, 100f, Creatures);
            }

            if (CombatDamageTakenGuid.Updated(reader))
            {
                Update(CombatDamageTakenGuid.Value, 100f, DamageTaken);
            }

            if (CombatDamageDoneGuid.Updated(reader))
            {
                Update(CombatDamageDoneGuid.Value, 100f, DamageDone);
            }

            if (CombatDeadGuid.Updated(reader))
            {
                Update(CombatDeadGuid.Value, 0, Creatures);
                Update(CombatDeadGuid.Value, 0, Deads);

                if (DamageTaken.Exists(x => x.Guid == CombatDeadGuid.Value))
                {
                    Update(CombatDeadGuid.Value, 0, DamageTaken);
                }

                if (DamageDone.Exists(x => x.Guid == CombatDeadGuid.Value))
                {
                    Update(CombatDeadGuid.Value, 0, DamageDone);
                }

                if (Targets.Exists(x => x.Guid == CombatDeadGuid.Value))
                {
                    Update(CombatDeadGuid.Value, 0, Targets);
                }

                if (Targets.Exists(x => x.Guid == CombatDeadGuid.Value) &&
                    (DamageDone.Exists(x => x.Guid == CombatDeadGuid.Value) || DamageTaken.Exists(x => x.Guid == CombatDeadGuid.Value)))
                {
                    KillCredit?.Invoke(this, EventArgs.Empty);
                }
            }

            RemoveExpired(Targets);
            RemoveExpired(Creatures);
            RemoveExpired(DamageTaken);
            RemoveExpired(DamageDone);
            RemoveExpired(Deads);
        }

        private static void Update(int creatureId, float healthPercent, List<CreatureRecord> CombatCreatures)
        {
            if (creatureId <= 0) return;

            int index = CombatCreatures.FindIndex(c => c.Guid == creatureId);
            if (index > -1)
            {
                if (healthPercent < CombatCreatures[index].HealthPercent)
                {
                    CreatureRecord creature = CombatCreatures[index];

                    creature.HealthPercent = healthPercent;
                    creature.LastEvent = DateTime.Now;

                    CombatCreatures[index] = creature;
                }
            }
            else
            {
                CombatCreatures.Add(new CreatureRecord
                {
                    Guid = creatureId,
                    HealthPercent = healthPercent,
                    LastEvent = DateTime.Now
                });
            }
        }

        private static void RemoveExpired(List<CreatureRecord> CombatCreatures)
        {
            CombatCreatures.RemoveAll(x => x.HasExpired(LifeTimeInSeconds));
        }
    }
}