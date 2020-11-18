using System;
using System.Linq;
using System.Collections.Generic;

namespace Libs
{
    public class CombatCreature
    {
        public int CreatureId { get; set; }

        public DateTime LastEvent { get; set; }

        public bool HasExpired()
        {
            return (DateTime.Now - LastEvent).TotalSeconds > 3;
        }

        public static void UpdateCombatCreatureCount(int creatureId, List<CombatCreature> CombatCreatures)
        {
            if (creatureId > 0)
            {
                var creature = CombatCreatures.Where(c => c.CreatureId == creatureId).FirstOrDefault();

                if (creature != null)
                {
                    creature.LastEvent = DateTime.Now;
                }
                else
                {
                    CombatCreatures.Add(new CombatCreature { CreatureId = creatureId, LastEvent = DateTime.Now });
                }
            }

            CombatCreatures.Where(c => c.HasExpired())
                .ToList()
                .ForEach(c => CombatCreatures.Remove(c));
        }
    }
}