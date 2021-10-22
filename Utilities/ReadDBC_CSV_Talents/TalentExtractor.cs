using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ReadDBC_CSV_Talents
{
    public class TalentExtractor
    {
        public class TalenTabExtractor
        {
            public static List<string> columnIndexs = new List<string> {
                "ID",
                "Name_lang",
                "BackgroundFile",
                "OrderIndex"
            };

            public static List<TalentTab> Extract(string srcFile)
            {
                var idIndex = FindIndex(columnIndexs, "ID");
                var NameIndex = FindIndex(columnIndexs, "Name_lang");
                var BackgroundFileIndex = FindIndex(columnIndexs, "BackgroundFile");
                var orderIndex = FindIndex(columnIndexs, "OrderIndex");

                var talenttabs = new List<TalentTab>();
                Action<string> extractLine = line =>
                {
                    var values = line.Split(",");
                    if (values.Length > idIndex && values.Length > orderIndex)
                    {
                        //Console.WriteLine($"{values[entryIndex]} - {values[nameIndex]}");
                        talenttabs.Add(new TalentTab
                        {
                            Id = int.Parse(values[idIndex]),
                            Name = values[NameIndex],
                            BackgroundFile = values[BackgroundFileIndex],
                            OrderIndex = int.Parse(values[orderIndex])
                        });
                    }
                };

                ExtractTemplate(srcFile, extractLine);

                Console.WriteLine($"TalentTab: {talenttabs.Count}");

                return talenttabs;
            }
        }

        public class TalentTreeExtractor
        {
            public static List<string> columnIndexs = new List<string> {
                "ID",
                "Description_lang",
                "TierID",
                "Flags",
                "ColumnIndex",
                "TabID",
                "ClassID",
                "SpecID",
                "SpellID",
                "OverridesSpellID",
                "RequiredSpellID",
                "CategoryMask[0]",
                "CategoryMask[1]",
                "SpellRank[0]",
                "SpellRank[1]",
                "SpellRank[2]",
                "SpellRank[3]",
                "SpellRank[4]"
            };

            public static List<Talent> Extract(string srcFile)
            {
                var idIndex = FindIndex(columnIndexs, "ID");

                var tierIDIndex = FindIndex(columnIndexs, "TierID");
                var columnIndex = FindIndex(columnIndexs, "ColumnIndex");
                var tabIDIndex = FindIndex(columnIndexs, "TabID");

                var spellRank0Index = FindIndex(columnIndexs, "SpellRank[0]");
                var spellRank1Index = FindIndex(columnIndexs, "SpellRank[1]");
                var spellRank2Index = FindIndex(columnIndexs, "SpellRank[2]");
                var spellRank3Index = FindIndex(columnIndexs, "SpellRank[3]");
                var spellRank4Index = FindIndex(columnIndexs, "SpellRank[4]");

                var talents = new List<Talent>();
                Action<string> extractLine = line =>
                {
                    var values = line.Split(",");
                    if (values.Length > idIndex && values.Length > spellRank4Index)
                    {
                        //Console.WriteLine($"{values[entryIndex]} - {values[nameIndex]}");
                        talents.Add(new Talent
                        {
                            TierID = int.Parse(values[tierIDIndex]),
                            ColumnIndex = int.Parse(values[columnIndex]),
                            TabID = int.Parse(values[tabIDIndex]),

                            SpellIds = new int[5]
                            {
                                 int.Parse(values[spellRank0Index]),
                                 int.Parse(values[spellRank1Index]),
                                 int.Parse(values[spellRank2Index]),
                                 int.Parse(values[spellRank3Index]),
                                 int.Parse(values[spellRank4Index])
                            }
                        });
                    }
                };

                ExtractTemplate(srcFile, extractLine);

                Console.WriteLine($"Talents: {talents.Count}");

                return talents;
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

            var talenttab = Path.Join(path, "talenttab.csv");
            var talenttabs = TalenTabExtractor.Extract(talenttab);
            File.WriteAllText(Path.Join(path, "talenttab.json"), JsonConvert.SerializeObject(talenttabs));

            var talent = Path.Join(path, "talent.csv");
            var talents = TalentTreeExtractor.Extract(talent);
            File.WriteAllText(Path.Join(path, "talent.json"), JsonConvert.SerializeObject(talents));
        }
    }
}
