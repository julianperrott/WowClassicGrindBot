using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    public class SpellBookReader
    {
        private readonly int cSpellId;

        private readonly ISquareReader reader;

        public HashSet<int> Spells { get; private set; } = new HashSet<int>();

        public SpellBookReader(ISquareReader reader, int cSpellId)
        {
            this.reader = reader;
            this.cSpellId = cSpellId;
        }

        public void Read()
        {
            int spellId = (int)reader.GetLongAtCell(cSpellId);
            Spells.Add(spellId);
        }

        public void Reset()
        {
            Spells.Clear();
        }
    }
}
