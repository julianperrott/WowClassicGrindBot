using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database;
using SharedLib;

namespace Core
{
    public class RequirementFactory
    {
        private readonly ILogger logger;
        private readonly PlayerReader playerReader;
        private readonly BagReader bagReader;
        private readonly SpellBookReader spellBookReader;
        private readonly TalentReader talentReader;
        private readonly CreatureDB creatureDb;
        private readonly ItemDB itemDb;

        private readonly Dictionary<string, Func<int>> valueDictionary = new Dictionary<string, Func<int>>();

        private readonly Dictionary<string, Func<bool>> booleanDictionary = new Dictionary<string, Func<bool>>();

        private readonly Dictionary<string, Func<string, Requirement>> keywordDictionary = new Dictionary<string, Func<string, Requirement>>();

        private readonly List<string> negate = new List<string>()
        {
           "not ",
           "!"
        };

        public RequirementFactory(ILogger logger, AddonReader addonReader, BagReader bagReader, EquipmentReader equipmentReader, SpellBookReader spellBookReader, TalentReader talentReader, CreatureDB creatureDb, ItemDB itemDb)
        {
            this.logger = logger;
            this.playerReader = addonReader.PlayerReader;
            this.bagReader = bagReader;
            this.spellBookReader = spellBookReader;
            this.talentReader = talentReader;
            this.creatureDb = creatureDb;
            this.itemDb = itemDb;

            keywordDictionary = new Dictionary<string, Func<string, Requirement>>()
            {
                { ">=", GetInclusiveValueBasedRequirement },
                { "<=", GetInclusiveValueBasedRequirement },
                { ">", GetExcusiveValueBasedRequirement },
                { "<", GetExcusiveValueBasedRequirement },
                { "==", GetEqualsValueBasedRequirement },
                { "npcID:", CreateNpcRequirement },
                { "BagItem:", CreateBagItemRequirement },
                { "SpellInRange:", CreateSpellInRangeRequirement },
                { "TargetCastingSpell", CreateTargetCastingSpellRequirement },
                { "Form", CreateFormRequirement },
                { "Race", CreateRaceRequirement },
                { "Spell", CreateSpellRequirement },
                { "Talent", CreateTalentRequirement },
                { "Trigger:", CreateTriggerRequirement }
            };

            booleanDictionary = new Dictionary<string, Func<bool>>
            {
                // Target Based
                { "TargetYieldXP", () => playerReader.TargetYieldXP },

                // Range
                { "InMeleeRange", ()=> playerReader.IsInMeleeRange },
                { "InDeadZoneRange", ()=> playerReader.IsInDeadZone },
                { "OutOfCombatRange", ()=> !playerReader.WithInCombatRange },
                { "InCombatRange", ()=> playerReader.WithInCombatRange },
                { "InFireblastRange", ()=> playerReader.SpellInRange.Mage_Fireblast },
                
                // Pet
                { "Has Pet", ()=> playerReader.Bits.HasPet },
                { "Pet Happy", ()=> playerReader.Bits.PetHappy },
                
                // Auto Spell
                { "AutoAttacking", ()=> playerReader.Bits.IsAutoRepeatSpellOn_AutoAttack },
                { "Shooting", ()=> playerReader.Bits.IsAutoRepeatSpellOn_Shoot },
                { "AutoShot", ()=> playerReader.Bits.IsAutoRepeatSpellOn_AutoShot },
                
                // Temporary Enchants
                { "HasMainHandEnchant", ()=> playerReader.Bits.MainHandEnchant_Active },
                { "HasOffHandEnchant", ()=> playerReader.Bits.OffHandEnchant_Active },
                
                // Equipment - Bag
                { "Items Broken", ()=> playerReader.Bits.ItemsAreBroken },
                { "BagFull", ()=> bagReader.BagsFull },
                { "HasRangedWeapon", ()=> equipmentReader.HasRanged() },
                { "HasAmmo", ()=> playerReader.Bits.HasAmmo },
                
                // General Buff Condition
                { "Eating", ()=> playerReader.Buffs.Eating },
                { "Drinking", ()=> playerReader.Buffs.Drinking },
                { "Mana Regeneration", ()=> playerReader.Buffs.ManaRegeneration },
                { "Well Fed", ()=> playerReader.Buffs.WellFed },
                { "Clearcasting", ()=> playerReader.Buffs.Clearcasting },

                // Player Affected
                { "Swimming", ()=> playerReader.Bits.IsSwimming },
                { "Falling", ()=> playerReader.Bits.IsFalling },

                //Priest
                { "Fortitude", ()=> playerReader.Buffs.Fortitude },
                { "InnerFire", ()=> playerReader.Buffs.InnerFire },
                { "Divine Spirit", ()=> playerReader.Buffs.DivineSpirit },
                { "Renew", ()=> playerReader.Buffs.Renew },
                { "Shield", ()=> playerReader.Buffs.Shield },

                // Druid
                { "Mark of the Wild", ()=> playerReader.Buffs.MarkOfTheWild },
                { "Thorns", ()=> playerReader.Buffs.Thorns },
                { "TigersFury", ()=> playerReader.Buffs.TigersFury },
                { "Prowl", ()=> playerReader.Buffs.Prowl },
                { "Rejuvenation", ()=> playerReader.Buffs.Rejuvenation },
                { "Regrowth", ()=> playerReader.Buffs.Regrowth },
                
                // Paladin
                { "Seal", ()=> playerReader.Buffs.Seal },
                { "Aura", ()=>playerReader.Buffs.Aura },
                { "Devotion Aura", ()=>playerReader.Buffs.Aura },
                { "Blessing", ()=> playerReader.Buffs.Blessing },
                { "Blessing of Might", ()=> playerReader.Buffs.Blessing },
                
                // Mage
                { "Frost Armor", ()=> playerReader.Buffs.FrostArmor },
                { "Ice Armor", ()=> playerReader.Buffs.FrostArmor },
                { "Arcane Intellect", ()=> playerReader.Buffs.ArcaneIntellect },
                { "Ice Barrier", ()=>playerReader.Buffs.IceBarrier },
                { "Ward", ()=>playerReader.Buffs.Ward },
                { "Fire Power", ()=>playerReader.Buffs.FirePower },
                { "Mana Shield", ()=>playerReader.Buffs.ManaShield },
                { "Presence of Mind", ()=>playerReader.Buffs.PresenceOfMind },
                { "Arcane Power", ()=>playerReader.Buffs.ArcanePower },
                
                // Rogue
                { "Slice and Dice", ()=> playerReader.Buffs.SliceAndDice },
                { "Stealth", ()=> playerReader.Buffs.Stealth },
                
                // Warrior
                { "Battle Shout", ()=> playerReader.Buffs.BattleShout },
                
                // Warlock
                { "Demon Skin", ()=> playerReader.Buffs.Demon },
                { "Demon Armor", ()=> playerReader.Buffs.Demon },
                { "Soul Link", ()=> playerReader.Buffs.SoulLink },
                { "Soulstone Resurrection", ()=> playerReader.Buffs.SoulstoneResurrection },
                { "Shadow Trance", ()=> playerReader.Buffs.ShadowTrance },
                
                // Shaman
                { "Lightning Shield", ()=> playerReader.Buffs.LightningShield },
                { "Water Shield", ()=> playerReader.Buffs.WaterShield },
                { "Shamanistic Focus", ()=> playerReader.Buffs.ShamanisticFocus },
                { "Focused", ()=> playerReader.Buffs.ShamanisticFocus },
                { "Stoneskin", ()=> playerReader.Buffs.Stoneskin },
                
                //Hunter
                { "Aspect of the Cheetah", ()=> playerReader.Buffs.Aspect },
                { "Aspect of the Pack", ()=> playerReader.Buffs.Aspect },
                { "Aspect of the Beast", ()=> playerReader.Buffs.Aspect },
                { "Aspect of the Hawk", ()=> playerReader.Buffs.Aspect },
                { "Aspect of the Wild", ()=> playerReader.Buffs.Aspect },
                { "Aspect of the Monkey", ()=> playerReader.Buffs.Aspect },
                { "Rapid Fire", ()=> playerReader.Buffs.RapidFire },
                { "Quick Shots", ()=> playerReader.Buffs.QuickShots },

                // Debuff Section
                // Druid Debuff
                { "Demoralizing Roar", ()=> playerReader.Debuffs.Roar },
                { "Faerie Fire", ()=> playerReader.Debuffs.FaerieFire },
                { "Rip", ()=> playerReader.Debuffs.Rip },
                { "Moonfire", ()=> playerReader.Debuffs.Moonfire },
                { "Entangling Roots", ()=> playerReader.Debuffs.EntanglingRoots },
                { "Rake", ()=> playerReader.Debuffs.Rake },
                
                // Warrior Debuff
                { "Rend", ()=> playerReader.Debuffs.Rend },
                
                // Priest Debuff
                { "Shadow Word: Pain", ()=> playerReader.Debuffs.ShadowWordPain },
                
                // Mage Debuff
                { "Frostbite", ()=> playerReader.Debuffs.Frostbite },
                { "Slow", ()=> playerReader.Debuffs.Slow },
                
                // Warlock Debuff
                { "Curse of Weakness", ()=> playerReader.Debuffs.Curseof },
                { "Curse of Elements", ()=> playerReader.Debuffs.Curseof },
                { "Curse of Recklessness", ()=> playerReader.Debuffs.Curseof },
                { "Curse of Shadow", ()=> playerReader.Debuffs.Curseof },
                { "Curse of Agony", ()=> playerReader.Debuffs.Curseof },
                { "Curse of", ()=> playerReader.Debuffs.Curseof },
                { "Corruption", ()=> playerReader.Debuffs.Corruption },
                { "Immolate", ()=> playerReader.Debuffs.Immolate },
                { "Siphon Life", ()=> playerReader.Debuffs.SiphonLife },
                
                // Hunter Debuff
                { "Serpent Sting", ()=> playerReader.Debuffs.SerpentSting },
            };

            valueDictionary = new Dictionary<string, Func<int>>
            {
                { "Health%", () => playerReader.HealthPercent },
                { "TargetHealth%", () => playerReader.TargetHealthPercentage },
                { "PetHealth%", () => playerReader.PetHealthPercentage },
                { "Mana%", () => playerReader.ManaPercentage },
                { "BagCount", () => bagReader.BagItems.Count },
                { "MobCount", () => addonReader.CombatCreatureCount },
                { "MinRange", () => playerReader.MinRange },
                { "MaxRange", () => playerReader.MaxRange },
                { "LastAutoShotMs", () => playerReader.AutoShot.ElapsedMs },
                { "LastMainHandMs", () => playerReader.MainHandSwing.ElapsedMs }
            };
        }

        public void InitialiseRequirements(KeyAction item)
        {
            CreateConsumableRequirement("Water", item);
            CreateConsumableRequirement("Food", item);

            item.RequirementObjects.Clear();
            foreach (string requirement in item.Requirements)
            {
                if (requirement.Contains("||"))
                {
                    Requirement orCombinedRequirement = new Requirement
                    {
                        LogMessage = () => ""
                    };
                    foreach (string part in requirement.Split("||"))
                    {
                        var sub = GetRequirement(item.Name, part);
                        orCombinedRequirement = orCombinedRequirement.Or(sub);
                    }

                    item.RequirementObjects.Add(orCombinedRequirement);
                }
                else if (requirement.Contains("&&"))
                {
                    Requirement andCombinedRequirement = new Requirement
                    {
                        LogMessage = () => ""
                    };
                    foreach (string part in requirement.Split("&&"))
                    {
                        var sub = GetRequirement(item.Name, part);
                        andCombinedRequirement = andCombinedRequirement.And(sub);
                    }

                    item.RequirementObjects.Add(andCombinedRequirement);
                }
                else
                {
                    item.RequirementObjects.Add(GetRequirement(item.Name, requirement));
                }
            }

            CreateMinRequirement(item.RequirementObjects, PowerType.Mana, item.MinMana);
            CreateMinRequirement(item.RequirementObjects, PowerType.Rage, item.MinRage);
            CreateMinRequirement(item.RequirementObjects, PowerType.Energy, item.MinEnergy);

            CreateMinComboPointsRequirement(item.RequirementObjects, item);
            CreateTargetIsCastingRequirement(item.RequirementObjects, item);
            CreateActionUsableRequirement(item.RequirementObjects, item);

            CreateCooldownRequirement(item.RequirementObjects, item);
            CreateChargeRequirement(item.RequirementObjects, item);
        }


        private void CreateTargetIsCastingRequirement(List<Requirement> itemRequirementObjects, KeyAction item)
        {
            if (item.UseWhenTargetIsCasting != null)
            {
                itemRequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => playerReader.IsTargetCasting == item.UseWhenTargetIsCasting.Value,
                    LogMessage = () => "Target casting"
                });
            }
        }

        private void CreateMinRequirement(List<Requirement> RequirementObjects, PowerType type, int value)
        {
            if (value > 0)
            {
                if (type == PowerType.Mana)
                {
                    RequirementObjects.Add(new Requirement
                    {
                        HasRequirement = () => playerReader.ManaCurrent >= value || playerReader.Buffs.Clearcasting,
                        LogMessage = () => $"{type} {playerReader.ManaCurrent} >= {value}"
                    });
                }
                else
                {
                    RequirementObjects.Add(new Requirement
                    {
                        HasRequirement = () => playerReader.PTCurrent >= value || playerReader.Buffs.Clearcasting,
                        LogMessage = () => $"{type} {playerReader.PTCurrent} >= {value}"
                    });
                }
            }
        }

        private void CreateMinComboPointsRequirement(List<Requirement> RequirementObjects, KeyAction item)
        {
            if (item.MinComboPoints > 0)
            {
                RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => playerReader.ComboPoints >= item.MinComboPoints,
                    LogMessage = () => $"Combo point {playerReader.ComboPoints} >= {item.MinComboPoints}"
                });
            }
        }

        private void CreateActionUsableRequirement(List<Requirement> RequirementObjects, KeyAction item)
        {
            if (item.WhenUsable && !string.IsNullOrEmpty(item.Key))
            {
                RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => 
                        !item.HasFormRequirement() ? playerReader.UsableAction.Is(item) :
                        (playerReader.Form == item.FormEnum && playerReader.UsableAction.Is(item)) ||
                        (playerReader.Form != item.FormEnum && item.CanDoFormChangeAndHaveMinimumMana()),

                    LogMessage = () => 
                        !item.HasFormRequirement() ? $"Usable" : // {playerReader.UsableAction.Num(item)}
                        (playerReader.Form != item.FormEnum && item.CanDoFormChangeAndHaveMinimumMana()) ? $"Usable after Form change" : // {playerReader.UsableAction.Num(item)}
                        (playerReader.Form == item.FormEnum && playerReader.UsableAction.Is(item)) ? $"Usable current Form" : $"not Usable current Form" // {playerReader.UsableAction.Num(item)}
                });
            }
        }

        private static void CreateCooldownRequirement(List<Requirement> RequirementObjects, KeyAction item)
        {
            if (item.Cooldown > 0)
            {
                RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => item.GetCooldownRemaining() == 0,
                    LogMessage = () => $"Cooldown {item.GetCooldownRemaining() / 1000:F1}",
                    VisibleIfHasRequirement = false
                });
            }
        }

        private static void CreateChargeRequirement(List<Requirement> RequirementObjects, KeyAction item)
        {
            if (item.Charge > 1)
            {
                RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => item.GetChargeRemaining() != 0,
                    LogMessage = () => $"Charge {item.GetChargeRemaining()}",
                    VisibleIfHasRequirement = true
                });
            }
        }

        private static void CreateConsumableRequirement(string name, KeyAction item)
        {
            if (item.Name == name)
            {
                item.StopBeforeCast = true;
                item.WhenUsable = true;
                item.AfterCastWaitBuff = true;

                item.Requirements.Add("not Swimming");
                item.Requirements.Add("not Falling");
            }
        }


        public Requirement GetRequirement(string name, string requirement)
        {
            var requirementText = requirement;
            logger.LogInformation($"[{name}] Processing requirement: \"{requirementText}\"");

            bool negated = false;
            string negateKeyword = negate.FirstOrDefault(x => requirement.StartsWith(x));
            if (!string.IsNullOrEmpty(negateKeyword))
            {
                requirement = requirement.Substring(negateKeyword.Length);
                negated = true;
            }

            string? key = keywordDictionary.Keys.FirstOrDefault(x => requirement.Contains(x));
            if (!string.IsNullOrEmpty(key))
            {
                var requirementObj = keywordDictionary[key](requirement);
                return negated ? requirementObj.Negate(negateKeyword) : requirementObj;
            }

            if (booleanDictionary.Keys.Contains(requirement))
            {
                var requirementObj = new Requirement
                {
                    HasRequirement = booleanDictionary[requirement],
                    LogMessage = () => $"{requirement}"
                };
                return negated ? requirementObj.Negate(negateKeyword) : requirementObj;
            }

            logger.LogInformation($"UNKNOWN REQUIREMENT! {requirement}: try one of: {string.Join(", ", booleanDictionary.Keys)}");
            return new Requirement
            {
                HasRequirement = () => false,
                LogMessage = () => $"UNKNOWN REQUIREMENT! {requirement}"
            };
        }


        private Requirement CreateTargetCastingSpellRequirement(string requirement)
        {
            if (requirement.Contains(":"))
            {
                var parts = requirement.Split(":");
                var spellsPart = parts[1].Split("|");
                var spellIds = spellsPart.Select(x => int.Parse(x.Trim())).ToArray();

                var spellIdsStringFormatted = string.Join(", ", spellIds);

                return new Requirement
                {
                    HasRequirement = () => spellIds.Contains(playerReader.SpellBeingCastByTarget),
                    LogMessage = () =>
                        $"Target casting {playerReader.SpellBeingCastByTarget} ∈ [{spellIdsStringFormatted}]"
                };
            }
            else
            {
                return new Requirement
                {
                    HasRequirement = () => playerReader.IsTargetCasting,
                    LogMessage = () => "Target casting"
                };
            }
        }

        private Requirement CreateFormRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var form = Enum.Parse<Form>(parts[1]);

            return new Requirement
            {
                HasRequirement = () => playerReader.Form == form,
                LogMessage = () => $"{playerReader.Form}"
            };
        }

        private Requirement CreateRaceRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var race = Enum.Parse<RaceEnum>(parts[1]);

            return new Requirement
            {
                HasRequirement = () => playerReader.Race == race,
                LogMessage = () => $"{playerReader.Race}"
            };
        }

        private Requirement CreateSpellRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var spellName = parts[1];

            if (int.TryParse(parts[1], out int spellId) && spellBookReader.SpellDB.Spells.TryGetValue(spellId, out Spell spell))
            {
                spellName = $"{spell.Name}({spellId})";
            }
            else
            {
                spellId = spellBookReader.GetSpellIdByName(spellName);
            }

            return new Requirement
            {
                HasRequirement = () => spellBookReader.Spells.ContainsKey(spellId),
                LogMessage = () => $"Spell {spellName}"
            };
        }

        private Requirement CreateTalentRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var talentName = parts[1];
            var rank = parts.Length < 3 ? 1 : int.Parse(parts[2]);

            return new Requirement
            {
                HasRequirement = () => talentReader.HasTalent(talentName, rank),
                LogMessage = () => rank == 1 ? $"Talent {talentName}" : $"Talent {talentName} (Rank {rank})"
            };
        }

        private Requirement CreateTriggerRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            int bit = int.Parse(parts[1]);
            string text = parts.Length > 2 ? parts[2] : string.Empty;

            return new Requirement
            {
                HasRequirement = () => playerReader.CustomTrigger1.IsBitSet(bit),
                LogMessage = () => $"Trigger({bit}) {text}"
            };
        }

        private Requirement CreateNpcRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var npcId = int.Parse(parts[1]);

            string npcName = string.Empty;
            if (creatureDb.Entries.TryGetValue(npcId, out Creature creature))
            {
                npcName = creature.Name;
            }

            return new Requirement
            {
                HasRequirement = () => playerReader.TargetId == npcId,
                LogMessage = () => $"TargetID {npcName}({npcId})"
            };
        }

        private Requirement CreateBagItemRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var itemId = int.Parse(parts[1]);
            var count = parts.Length < 3 ? 1 : int.Parse(parts[2]);

            var itemName = string.Empty;
            if (itemDb.Items.TryGetValue(itemId, out Item item))
            {
                itemName = item.Name;
            }

            return new Requirement
            {
                HasRequirement = () => bagReader.ItemCount(itemId) >= count,
                LogMessage = () => count == 1 ? $"in bag {itemName}({itemId})" : $"{itemName}({itemId}) count >= {count}"
            };
        }

        private Requirement CreateSpellInRangeRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var bitId = int.Parse(parts[1]);

            return new Requirement
            {
                HasRequirement = () => playerReader.SpellInRange.IsBitSet(bitId),
                LogMessage = () => $"SpellInRange {bitId}"
            };
        }


        private Requirement GetExcusiveValueBasedRequirement(string requirement)
        {
            var symbol = "<";
            if (requirement.Contains(">"))
            {
                symbol = ">";
            }

            var parts = requirement.Split(symbol);
            var value = int.Parse(parts[1]);

            if (!valueDictionary.Keys.Contains(parts[0]))
            {
                logger.LogInformation($"UNKNOWN REQUIREMENT! {requirement}: try one of: {string.Join(", ", valueDictionary.Keys)}");
                return new Requirement
                {
                    HasRequirement = () => false,
                    LogMessage = () => $"UNKNOWN REQUIREMENT! {requirement}"
                };
            }

            var valueCheck = valueDictionary[parts[0]];
            if (symbol == ">")
            {
                return new Requirement
                {
                    HasRequirement = () => valueCheck() > value,
                    LogMessage = () => $"{parts[0]} {valueCheck()} > {value}"
                };
            }
            else
            {
                return new Requirement
                {
                    HasRequirement = () => valueCheck() < value,
                    LogMessage = () => $"{parts[0]} {valueCheck()} < {value}"
                };
            }
        }

        private Requirement GetInclusiveValueBasedRequirement(string requirement)
        {
            var symbol = "<=";
            if (requirement.Contains(">="))
            {
                symbol = ">=";
            }

            var parts = requirement.Split(symbol);
            var value = int.Parse(parts[1]);

            if (!valueDictionary.Keys.Contains(parts[0]))
            {
                logger.LogInformation($"UNKNOWN REQUIREMENT! {requirement}: try one of: {string.Join(", ", valueDictionary.Keys)}");
                return new Requirement
                {
                    HasRequirement = () => false,
                    LogMessage = () => $"UNKNOWN REQUIREMENT! {requirement}"
                };
            }

            var valueCheck = valueDictionary[parts[0]];
            if (symbol == ">=")
            {
                return new Requirement
                {
                    HasRequirement = () => valueCheck() >= value,
                    LogMessage = () => $"{parts[0]} {valueCheck()} >= {value}"
                };
            }
            else
            {
                return new Requirement
                {
                    HasRequirement = () => valueCheck() <= value,
                    LogMessage = () => $"{parts[0]} {valueCheck()} <= {value}"
                };
            }
        }

        private Requirement GetEqualsValueBasedRequirement(string requirement)
        {
            var symbol = "==";
            var parts = requirement.Split(symbol);
            var value = int.Parse(parts[1]);

            if (!valueDictionary.Keys.Contains(parts[0]))
            {
                logger.LogInformation($"UNKNOWN REQUIREMENT! {requirement}: try one of: {string.Join(", ", valueDictionary.Keys)}");
                return new Requirement
                {
                    HasRequirement = () => false,
                    LogMessage = () => $"UNKNOWN REQUIREMENT! {requirement}"
                };
            }

            var valueCheck = valueDictionary[parts[0]];
            return new Requirement
            {
                HasRequirement = () => valueCheck() == value,
                LogMessage = () => $"{parts[0]} {valueCheck()} == {value}"
            };
        }

    }
}