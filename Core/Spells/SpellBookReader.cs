using System;
using System.Collections.Generic;
using System.Text;
using Core.Database;

namespace Core
{
    public class SpellBookReader
    {
        private readonly int cSpellId;

        private readonly ISquareReader reader;
        public SpellDB SpellDB { private set; get; }

        public Dictionary<int, Spell> Spells { get; private set; } = new Dictionary<int, Spell>();

        public SpellBookReader(ISquareReader reader, int cSpellId, SpellDB spellDB)
        {
            this.reader = reader;
            this.cSpellId = cSpellId;
            this.SpellDB = spellDB;
        }

        public void Read()
        {
            int spellId = (int)reader.GetLongAtCell(cSpellId);
            if (!Spells.ContainsKey(spellId) && SpellDB.Spells.TryGetValue(spellId, out Spell spell))
            {
                Spells.Add(spellId, spell);
            }
        }

        public void Reset()
        {
            Spells.Clear();
        }

        public int GetSpellIdByName(string name)
        {
            foreach (var kvp in Spells)
            {
                if (kvp.Value.Name.ToLower() == name.ToLower())
                    return kvp.Key;
            }

            return 0;
        }
    }
}
