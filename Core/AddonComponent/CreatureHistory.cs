using System;
using System.Linq;
using System.Collections.Generic;

namespace Core
{
    public class CreatureHistory
    {
        private readonly ISquareReader reader;

        private const int LifeTimeInSeconds = 60;

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
                Update(CombatDeadGuid.Value, 0, Deads);
                Update(CombatDeadGuid.Value, 0, Creatures);
                Update(CombatDeadGuid.Value, 0, DamageTaken);
                Update(CombatDeadGuid.Value, 0, DamageDone);

                // Update last target health from LastDeadGuid
                if (Targets.FindIndex(x => x.Guid == CombatDeadGuid.Value) != -1)
                {
                    Update(CombatDeadGuid.Value, 0, Targets);
                }
            }
        }


        private static void Update(int creatureId, float healthPercent, List<CreatureRecord> CombatCreatures)
        {
            if (creatureId > 0)
            {
                int index = CombatCreatures.FindIndex(c => c.Guid == creatureId);
                if (index > -1)
                {
                    CreatureRecord creature = CombatCreatures[index];

                    if (creature.HealthPercent > healthPercent)
                    {
                        creature.HealthPercent = healthPercent;
                    }
                    creature.LastEvent = DateTime.Now;

                    CombatCreatures[index] = creature;
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

            CombatCreatures.Where(c => c.HasExpired(LifeTimeInSeconds))
                .ToList()
                .ForEach(c => CombatCreatures.Remove(c));
        }
    }
}