using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApp10
{
    internal class Program
    {
        public class Creature
        {
            public int Entry { get; set; }
            public string Name { get; set; }

            public static List<string> columnIndexs = new List<string> { "Entry", "Name", "SubName", "MinLevel", "MaxLevel", "ModelId1", "ModelId2", "ModelId3", "ModelId4", "Faction", "Scale", "Family", "CreatureType", "InhabitType", "RegenerateStats", "RacialLeader", "NpcFlags", "UnitFlags", "DynamicFlags", "ExtraFlags", "CreatureTypeFlags", "SpeedWalk", "SpeedRun", "Detection", "CallForHelp", "Pursuit", "Leash", "Timeout", "UnitClass", "Rank", "HealthMultiplier", "PowerMultiplier", "DamageMultiplier", "DamageVariance", "ArmorMultiplier", "ExperienceMultiplier", "MinLevelHealth", "MaxLevelHealth", "MinLevelMana", "MaxLevelMana", "MinMeleeDmg", "MaxMeleeDmg", "MinRangedDmg", "MaxRangedDmg", "Armor", "MeleeAttackPower", "RangedAttackPower", "MeleeBaseAttackTime", "RangedBaseAttackTime", "DamageSchool", "MinLootGold", "MaxLootGold", "LootId", "PickpocketLootId", "SkinningLootId", "KillCredit1", "KillCredit2", "MechanicImmuneMask", "SchoolImmuneMask", "ResistanceHoly", "ResistanceFire", "ResistanceNature", "ResistanceFrost", "ResistanceShadow", "ResistanceArcane", "PetSpellDataId", "MovementType", "TrainerType", "TrainerSpell", "TrainerClass", "TrainerRace", "TrainerTemplateId", "VendorTemplateId", "GossipMenuId", "visibilityDistanceType", "EquipmentTemplateId", "Civilian", "AIName", "ScriptName" };

            public static void Extract(string file)
            {
                var entryIndex = FindIndex(columnIndexs, "Entry");
                var nameIndex = FindIndex(columnIndexs, "Name");

                var items = new List<Creature>();
                Action<string> extractLine = line =>
                {
                    var values = splitLine(line);
                    //Console.WriteLine($"{values[entryIndex]},{values[nameIndex]},{values[subNameIndex]}");

                    items.Add(new Creature
                    {
                        Name = values[nameIndex],
                        Entry = int.Parse(values[entryIndex].Replace("(", ""))
                    });
                };

                ExtractItemTemplateTBC(file, "creature_template", extractLine);

                Console.WriteLine($"Creatures {items.Count}");

                File.WriteAllText(@"creatures.json", JsonConvert.SerializeObject(items));
            }
        }

        private static void Main(string[] args)
        {
            string file = @"..\..\..\..\data\TBCDB_1.8.0_VengeanceStrikesBack.sql";

            Creature.Extract(file);
            Console.ReadLine();
        }

        private static void ExtractItemTemplateTBC(string file, string tablename, Action<string> extractLine)
        {
            var stream = File.OpenText(file);

            var line = stream.ReadLine();

            while (line != null)
            {
                line = line.Trim();

                string beginTemplate = $"INSERT INTO `{tablename}` VALUES ";

                if (line.StartsWith(beginTemplate))
                {
                    var rx = new Regex(@"\(\d.*?\)(,|;)");
                    MatchCollection matches = rx.Matches(line);
                    foreach(var match in matches)
                    {
                        extractLine(match.ToString());
                    }
                }

                line = stream.ReadLine();
            }
        }

        private static string[] splitLine(string line)
        {
            var result = new List<string>();

            line = line.Replace(@"\'", "'");
            line = line.Replace(@"\'", "'");
            line = line.Replace(@"\", "");

            var isInString = false;
            var startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (isInString)
                {
                    if (line.Substring(i, 1) == "'")
                    {
                        isInString = false;
                        continue;
                    }
                }
                else
                {
                    if (line.Substring(i, 1) == "'" && i == startIndex)
                    {
                        isInString = true;
                    }

                    if (line.Substring(i, 1) == ",")
                    {
                        var value = line.Substring(startIndex, i - startIndex);
                        if (value.StartsWith("'"))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        result.Add(value);
                        startIndex = i + 1;
                    }
                }
            }
            return result.ToArray();
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
    }
}