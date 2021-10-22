using System;

namespace ReadDBC_CSV_Spell
{
    class Program
    {
        static void Main(string[] args)
        {
            var spellExtractor = new SpellExtractor();
            spellExtractor.Generate();
        }
    }
}
