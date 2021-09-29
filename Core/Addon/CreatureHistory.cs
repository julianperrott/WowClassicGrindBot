using System;
using System.Linq;
using System.Collections.Generic;

namespace Core
{
    public class CreatureHistory
    {
        public int CreatureId { get; set; }

        public float LastKnownHealthPercent { get; set; }

        public DateTime LastEvent { get; set; }

        public bool HasExpired()
        {
            return (DateTime.Now - LastEvent).TotalSeconds > 60;
        }

        public static void Update(int creatureId, float healthPercent, List<CreatureHistory> CombatCreatures)
        {
            if (creatureId > 0)
            {
                var creature = CombatCreatures.Where(c => c.CreatureId == creatureId).FirstOrDefault();

                if (creature != null)
                {
                    creature.LastKnownHealthPercent = healthPercent;
                    creature.LastEvent = DateTime.Now;
                }
                else
                {
                    CombatCreatures.Add(new CreatureHistory { CreatureId = creatureId, LastKnownHealthPercent = healthPercent, LastEvent = DateTime.Now });
                }
            }

            CombatCreatures.Where(c => c.HasExpired())
                .ToList()
                .ForEach(c => CombatCreatures.Remove(c));
        }

        public override string ToString()
        {
            return $"id: {CreatureId} | hp: {LastKnownHealthPercent}";
        }
    }
}