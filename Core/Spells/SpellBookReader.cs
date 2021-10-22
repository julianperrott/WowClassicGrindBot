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
        private readonly SpellDB spellDB;

        public Dictionary<int, Spell> Spells { get; private set; } = new Dictionary<int, Spell>();

        public SpellBookReader(ISquareReader reader, int cSpellId, SpellDB spellDB)
        {
            this.reader = reader;
            this.cSpellId = cSpellId;
            this.spellDB = spellDB;
        }

        public void Read()
        {
            int spellId = (int)reader.GetLongAtCell(cSpellId);
            if (!Spells.ContainsKey(spellId) && spellDB.Spells.TryGetValue(spellId, out Spell spell))
            {
                Spells.Add(spellId, spell);
            }
        }

        public void Reset()
        {
            Spells.Clear();
        }
    }
}
