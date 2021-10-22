using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ReadDBC_CSV_Spell
{
    public class SpellExtractor
    {
        public class SpellNameExtractor
        {
            public static List<string> columnIndexs = new List<string> {
                "ID", "Name_lang"
            };

            public static List<Spell> Extract(string srcFile)
            {
                var entryIndex = FindIndex(columnIndexs, "ID");
                var nameIndex = FindIndex(columnIndexs, "Name_lang");

                var spells = new List<Spell>();
                Action<string> extractLine = line =>
                {
                    var values = line.Split(",");
                    if (values.Length > entryIndex && values.Length > nameIndex)
                    {
                        //Console.WriteLine($"{values[entryIndex]} - {values[nameIndex]}");
                        spells.Add(new Spell
                        {
                            Id = int.Parse(values[entryIndex]),
                            Name = values[nameIndex]
                        });
                    }
                };

                ExtractTemplate(srcFile, extractLine);

                //Console.WriteLine($"Spell Names\n{string.Join("\n", items)}\n");
                Console.WriteLine($"Spell Names: {spells.Count}");

                return spells;
            }
        }

        public class SpellLevelExtractor
        {
            public static List<string> columnIndexs = new List<string> {
                "ID","DifficultyID","BaseLevel","MaxLevel","SpellLevel","MaxPassiveAuraLevel","SpellID"
            };

            public static void ExtractUpdate(List<Spell> spells, string srcFile)
            {
                var entryIndex = FindIndex(columnIndexs, "ID");
                var spellIdIndex = FindIndex(columnIndexs, "SpellID");
                var BaseLevelIndex = FindIndex(columnIndexs, "BaseLevel");

                Action<string> extractLine = line =>
                {
                    var values = line.Split(",");
                    if (values.Length > entryIndex && values.Length > spellIdIndex)
                    {
                        if (int.TryParse(values[spellIdIndex], out int spellId))
                        {
                            int index = spells.FindIndex(0, x => x.Id == spellId);
                            if (index > 0)
                            {
                                //Console.WriteLine($"{values[spellIdIndex]} - {values[BaseLevelIndex]}");
                                spells[index].Level = int.Parse(values[BaseLevelIndex]);
                            }
                        }
                    }
                };

                ExtractTemplate(srcFile, extractLine);
            }
        }

        private static void ExtractTemplate(string file, Action<string> extractLine)
        {
            var stream = File.OpenText(file);

            // header
            var line = stream.ReadLine();

            // data
            line = stream.ReadLine();
            while (line != null)
            {
                extractLine(line);
                line = stream.ReadLine();
            }
        }

        private static int FindIndex(List<string> columnIndexs, string v)
        {
            for (int i = 0; i < columnIndexs.Count; i++)
            {
                if (columnIndexs[i] == v)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException(v);
        }


        public void Generate()
        {
            var path = "../../../data/";
            var spellname = Path.Join(path, "spellname.csv");

            var spells = SpellNameExtractor.Extract(spellname);

            var spelllevels = Path.Join(path, "spelllevels.csv");
            SpellLevelExtractor.ExtractUpdate(spells, spelllevels);

            File.WriteAllText(Path.Join(path, "spells.json"), JsonConvert.SerializeObject(spells));
        }
    }
}
