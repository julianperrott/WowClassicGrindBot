using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApp10
{
    internal class Program
    {
        public class Item
        {
            public int Entry { get; set; }
            public string Name { get; set; }
            public int Quality { get; set; }
            public int SellPrice { get; set; }

            public static List<string> columnIndexs = new List<string> { "entry", "class", "subclass", "unk0", "name", "displayid", "Quality", "Flags", "BuyCount", "BuyPrice", "SellPrice", "InventoryType", "AllowableClass", "AllowableRace", "ItemLevel", "RequiredLevel", "RequiredSkill", "RequiredSkillRank", "requiredspell", "requiredhonorrank", "RequiredCityRank", "RequiredReputationFaction", "RequiredReputationRank", "maxcount", "stackable", "ContainerSlots", "stat_type1", "stat_value1", "stat_type2", "stat_value2", "stat_type3", "stat_value3", "stat_type4", "stat_value4", "stat_type5", "stat_value5", "stat_type6", "stat_value6", "stat_type7", "stat_value7", "stat_type8", "stat_value8", "stat_type9", "stat_value9", "stat_type10", "stat_value10", "dmg_min1", "dmg_max1", "dmg_type1", "dmg_min2", "dmg_max2", "dmg_type2", "dmg_min3", "dmg_max3", "dmg_type3", "dmg_min4", "dmg_max4", "dmg_type4", "dmg_min5", "dmg_max5", "dmg_type5", "armor", "holy_res", "fire_res", "nature_res", "frost_res", "shadow_res", "arcane_res", "delay", "ammo_type", "RangedModRange", "spellid_1", "spelltrigger_1", "spellcharges_1", "spellppmRate_1", "spellcooldown_1", "spellcategory_1", "spellcategorycooldown_1", "spellid_2", "spelltrigger_2", "spellcharges_2", "spellppmRate_2", "spellcooldown_2", "spellcategory_2", "spellcategorycooldown_2", "spellid_3", "spelltrigger_3", "spellcharges_3", "spellppmRate_3", "spellcooldown_3", "spellcategory_3", "spellcategorycooldown_3", "spellid_4", "spelltrigger_4", "spellcharges_4", "spellppmRate_4", "spellcooldown_4", "spellcategory_4", "spellcategorycooldown_4", "spellid_5", "spelltrigger_5", "spellcharges_5", "spellppmRate_5", "spellcooldown_5", "spellcategory_5", "spellcategorycooldown_5", "bonding", "description", "PageText", "LanguageID", "PageMaterial", "startquest", "lockid", "Material", "sheath", "RandomProperty", "block", "itemset", "MaxDurability", "area", "Map", "BagFamily", "ScriptName", "DisenchantID", "FoodType", "minMoneyLoot", "maxMoneyLoot", "Duration", "ExtraFlags" };

            public static void Extract(string file)
            {
                var entryIndex = FindIndex(columnIndexs,"entry");
                var nameIndex = FindIndex(columnIndexs, "name");
                var qualityIndex = FindIndex(columnIndexs, "Quality");
                var sellPriceIndex = FindIndex(columnIndexs,"SellPrice");

                var items = new List<Item>();
                Action<string> extractLine = line =>
                {
                    var values = splitLine(line);
                    //Console.WriteLine($"{values[entryIndex]},{values[nameIndex]},{values[qualityIndex]},{values[sellPriceIndex]}");

                    items.Add(new Item
                    {
                        Name = values[nameIndex],
                        Entry = int.Parse(values[entryIndex].Replace("(", "")),
                        Quality = int.Parse(values[qualityIndex]),
                        SellPrice = int.Parse(values[sellPriceIndex])
                    });
                };

                ExtractItemTemplateTBC(file, "item_template", extractLine);

                Console.WriteLine($"Items {items.Count}");

                File.WriteAllText(@"items.json", JsonConvert.SerializeObject(items));
            }
        }

        public class Creature
        {
            public int Entry { get; set; }
            public string Name { get; set; }
            public string SubName { get; set; }

            public static List<string> columnIndexs = new List<string> { "Entry", "Name", "SubName", "MinLevel", "MaxLevel", "ModelId1", "ModelId2", "ModelId3", "ModelId4", "Faction", "Scale", "Family", "CreatureType", "InhabitType", "RegenerateStats", "RacialLeader", "NpcFlags", "UnitFlags", "DynamicFlags", "ExtraFlags", "CreatureTypeFlags", "SpeedWalk", "SpeedRun", "Detection", "CallForHelp", "Pursuit", "Leash", "Timeout", "UnitClass", "Rank", "HealthMultiplier", "PowerMultiplier", "DamageMultiplier", "DamageVariance", "ArmorMultiplier", "ExperienceMultiplier", "MinLevelHealth", "MaxLevelHealth", "MinLevelMana", "MaxLevelMana", "MinMeleeDmg", "MaxMeleeDmg", "MinRangedDmg", "MaxRangedDmg", "Armor", "MeleeAttackPower", "RangedAttackPower", "MeleeBaseAttackTime", "RangedBaseAttackTime", "DamageSchool", "MinLootGold", "MaxLootGold", "LootId", "PickpocketLootId", "SkinningLootId", "KillCredit1", "KillCredit2", "MechanicImmuneMask", "SchoolImmuneMask", "ResistanceHoly", "ResistanceFire", "ResistanceNature", "ResistanceFrost", "ResistanceShadow", "ResistanceArcane", "PetSpellDataId", "MovementType", "TrainerType", "TrainerSpell", "TrainerClass", "TrainerRace", "TrainerTemplateId", "VendorTemplateId", "GossipMenuId", "visibilityDistanceType", "EquipmentTemplateId", "Civilian", "AIName", "ScriptName" };

            public static void Extract(string file)
            {
                var entryIndex = FindIndex(columnIndexs, "Entry");
                var nameIndex = FindIndex(columnIndexs, "Name");
                var subNameIndex = FindIndex(columnIndexs, "SubName");

                var items = new List<Creature>();
                Action<string> extractLine = line =>
                {
                    var values = splitLine(line);
                    //Console.WriteLine($"{values[entryIndex]},{values[nameIndex]},{values[subNameIndex]}");

                    items.Add(new Creature
                    {
                        Name = values[nameIndex],
                        Entry = int.Parse(values[entryIndex].Replace("(", "")),
                        SubName = values[subNameIndex]
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

            Item.Extract(file);
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