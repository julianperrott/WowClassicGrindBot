using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class RequirementFactory
    {
        private readonly PlayerReader playerReader;
        private readonly BagReader bagReader;
        private readonly EquipmentReader equipmentReader;
        private readonly ILogger logger;
        private Dictionary<string, Func<bool>> BuffDictionary = new Dictionary<string, Func<bool>>();

        public RequirementFactory(PlayerReader playerReader, BagReader bagReader, EquipmentReader equipmentReader, ILogger logger)
        {
            this.playerReader = playerReader;
            this.bagReader = bagReader;
            this.equipmentReader = equipmentReader;
            this.logger = logger;
        }

        public void InitialiseRequirements(KeyAction item)
        {
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
                        var sub = GetRequirement(item.Name, part, item.FormEnum);
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
                        var sub = GetRequirement(item.Name, part, item.FormEnum);
                        andCombinedRequirement = andCombinedRequirement.And(sub);
                    }

                    item.RequirementObjects.Add(andCombinedRequirement);
                }
                else
                {
                    item.RequirementObjects.Add(GetRequirement(item.Name, requirement, item.FormEnum));
                }
            }

            CreateMinRequirement(item.RequirementObjects, "Mana", item.MinMana);
            CreateMinRequirement(item.RequirementObjects, "Rage", item.MinRage);
            CreateMinRequirement(item.RequirementObjects, "Energy", item.MinEnergy);

            CreateConsumableRequirement("Water", item);
            CreateConsumableRequirement("Food", item);

            CreateMinComboPointsRequirement(item.RequirementObjects, item);
            CreateTargetIsCastingRequirement(item.RequirementObjects, item.UseWhenTargetIsCasting);
            CreateActionUsableRequirement(item.RequirementObjects, item);

            item.CreateCooldownRequirement();
            item.CreateChargeRequirement();
        }

        private void CreateTargetIsCastingRequirement(List<Requirement> itemRequirementObjects, bool? value)
        {
            if (value != null)
            {
                itemRequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => playerReader.IsTargetCasting == value.Value,
                    LogMessage = () => "Target casting"
                });
            }
        }

        private void CreateMinRequirement(List<Requirement> RequirementObjects, string type, int value)
        {
            if (value > 0)
            {
                if (type == "Mana")
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

        private void CreateConsumableRequirement(string name, KeyAction item)
        {
            if (item.Name == name)
            {
                item.StopBeforeCast = true;
                item.WhenUsable = true;
                item.AfterCastWaitBuff = true;

                item.RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => !playerReader.PlayerBitValues.IsSwimming,
                    LogMessage = () => $"Not swim"
                });

                item.RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => !playerReader.PlayerBitValues.IsFalling,
                    LogMessage = () => $"Not falling"
                });
            }
        }

        public Requirement GetRequirement(string name, string requirement, Form form)
        {
            this.logger.LogInformation($"[{name}] Processing requirement: {requirement}");

            var requirementText = requirement;

            if (requirement.Contains(">") || requirement.Contains("<"))
            {
                return GetValueBasedRequirement(name, requirement, form);
            }

            if (requirement.Contains("npcID:"))
            {
                return CreateNpcRequirement(requirement);
            }

            if (requirement.Contains("BagItem:"))
            {
                return CreateBagItemRequirement(requirement);
            }

            if (requirement.Contains("SpellInRange:"))
            {
                return CreateSpellInRangeRequirement(requirement);
            }

            if (requirement.Contains("TargetCastingSpell"))
            {
                return CreateTargetCastingSpellRequirement(requirement);
            }

            if(requirement.Contains("Form"))
            {
                return CreateFormRequirement(requirement);
            }

            if (BuffDictionary.Count == 0)
            {
                BuffDictionary = new Dictionary<string, Func<bool>>
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
                    { "Has Pet", ()=> playerReader.PlayerBitValues.HasPet },
                    { "Pet Happy", ()=> playerReader.PlayerBitValues.PetHappy },
                    // Auto Spell
                    { "AutoAttacking", ()=> playerReader.IsAutoAttacking },
                    { "Shooting", ()=> playerReader.IsShooting },
                    { "AutoShot", ()=> playerReader.IsAutoShoting },
                    // Temporary Enchants
                    { "HasMainHandEnchant", ()=> playerReader.PlayerBitValues.MainHandEnchant_Active },
                    { "HasOffHandEnchant", ()=> playerReader.PlayerBitValues.OffHandEnchant_Active },
                    // Equipment - Bag
                    { "Items Broken", ()=> playerReader.PlayerBitValues.ItemsAreBroken },
                    { "BagFull", ()=> bagReader.BagsFull },
                    { "HasRangedWeapon", ()=> equipmentReader.HasRanged() },
                    { "HasAmmo", ()=> playerReader.PlayerBitValues.HasAmmo },
                    // General Buff Condition
                    {  "Eating", ()=> playerReader.Buffs.Eating },
                    {  "Drinking", ()=> playerReader.Buffs.Drinking },
                    {  "Mana Regeneration", ()=> playerReader.Buffs.ManaRegeneration },
                    {  "Well Fed", ()=> playerReader.Buffs.WellFed },
                    {  "Clearcasting", ()=> playerReader.Buffs.Clearcasting },
                    //Priest
                    {  "Fortitude", ()=> playerReader.Buffs.Fortitude },
                    {  "InnerFire", ()=> playerReader.Buffs.InnerFire },
                    {  "Divine Spirit", ()=> playerReader.Buffs.DivineSpirit },
                    {  "Renew", ()=> playerReader.Buffs.Renew },
                    {  "Shield", ()=> playerReader.Buffs.Shield },
                    // Druid
                    {  "Mark of the Wild", ()=> playerReader.Buffs.MarkOfTheWild },
                    {  "Thorns", ()=> playerReader.Buffs.Thorns },
                    {  "TigersFury", ()=> playerReader.Buffs.TigersFury },
                    {  "Prowl", ()=> playerReader.Buffs.Prowl },
                    {  "Rejuvenation", ()=> playerReader.Buffs.Rejuvenation },
                    {  "Regrowth", ()=> playerReader.Buffs.Regrowth },
                    // Paladin
                    {  "Seal", ()=> playerReader.Buffs.Seal },
                    {  "Aura", ()=>playerReader.Buffs.Aura },
                    {  "Devotion Aura", ()=>playerReader.Buffs.Aura },
                    {  "Blessing", ()=> playerReader.Buffs.Blessing },
                    {  "Blessing of Might", ()=> playerReader.Buffs.Blessing },
                    // Mage
                    {  "Frost Armor", ()=> playerReader.Buffs.FrostArmor },
                    {  "Ice Armor", ()=> playerReader.Buffs.FrostArmor },
                    {  "Arcane Intellect", ()=> playerReader.Buffs.ArcaneIntellect },
                    {  "Ice Barrier", ()=>playerReader.Buffs.IceBarrier },
                    {  "Ward", ()=>playerReader.Buffs.Ward },
                    {  "Fire Power", ()=>playerReader.Buffs.FirePower },
                    {  "Mana Shield", ()=>playerReader.Buffs.ManaShield },
                    {  "Presence of Mind", ()=>playerReader.Buffs.PresenceOfMind },
                    {  "Arcane Power", ()=>playerReader.Buffs.ArcanePower },
                    // Rogue
                    {  "Slice and Dice", ()=> playerReader.Buffs.SliceAndDice },
                    {  "Stealth", ()=> playerReader.Buffs.Stealth },
                    // Warrior
                    {  "Battle Shout", ()=> playerReader.Buffs.BattleShout },
                    // Warlock
                    {  "Demon Skin", ()=> playerReader.Buffs.Demon },
                    {  "Demon Armor", ()=> playerReader.Buffs.Demon },
                    {  "Soul Link", ()=> playerReader.Buffs.SoulLink },
                    {  "Soulstone Resurrection", ()=> playerReader.Buffs.SoulstoneResurrection },
                    {  "Shadow Trance", ()=> playerReader.Buffs.ShadowTrance },
                    // Shaman
                    {  "Lightning Shield", ()=> playerReader.Buffs.LightningShield },
                    {  "Water Shield", ()=> playerReader.Buffs.WaterShield },
                    {  "Shamanistic Focus", ()=> playerReader.Buffs.ShamanisticFocus },
                    //Hunter
                    {  "Aspect of the Cheetah", ()=> playerReader.Buffs.Aspect },
                    {  "Aspect of the Pack", ()=> playerReader.Buffs.Aspect },
                    {  "Aspect of the Beast", ()=> playerReader.Buffs.Aspect },
                    {  "Aspect of the Hawk", ()=> playerReader.Buffs.Aspect },
                    {  "Aspect of the Wild", ()=> playerReader.Buffs.Aspect },
                    {  "Aspect of the Monkey", ()=> playerReader.Buffs.Aspect },
                    {  "Rapid Fire", ()=> playerReader.Buffs.RapidFire },
                    {  "Quick Shots", ()=> playerReader.Buffs.QuickShots },

                    // Debuff Section
                    // Druid Debuff
                    {  "Demoralizing Roar", ()=> playerReader.Debuffs.Roar },
                    {  "Faerie Fire", ()=> playerReader.Debuffs.FaerieFire },
                    {  "Rip", ()=> playerReader.Debuffs.Rip },
                    {  "Moonfire", ()=> playerReader.Debuffs.Moonfire },
                    {  "Entangling Roots", ()=> playerReader.Debuffs.EntanglingRoots },
                    {  "Rake", ()=> playerReader.Debuffs.Rake },
                    // Warrior Debuff
                    {  "Rend", ()=> playerReader.Debuffs.Rend },
                    // Priest Debuff
                    {  "Shadow Word: Pain", ()=> playerReader.Debuffs.ShadowWordPain },
                    // Mage Debuff
                    { "Frostbite", ()=> playerReader.Debuffs.Frostbite },
                    { "Slow", ()=> playerReader.Debuffs.Slow },
                    // Warlock Debuff
                    {  "Curse of Weakness", ()=> playerReader.Debuffs.Curseof },
                    {  "Curse of Elements", ()=> playerReader.Debuffs.Curseof },
                    {  "Curse of Recklessness", ()=> playerReader.Debuffs.Curseof },
                    {  "Curse of Shadow", ()=> playerReader.Debuffs.Curseof },
                    {  "Curse of Agony", ()=> playerReader.Debuffs.Curseof },
                    {  "Curse of", ()=> playerReader.Debuffs.Curseof },
                    {  "Corruption", ()=> playerReader.Debuffs.Corruption },
                    {  "Immolate", ()=> playerReader.Debuffs.Immolate },
                    {  "Siphon Life", ()=> playerReader.Debuffs.SiphonLife },
                    // Hunter Debuff
                    {  "Serpent Sting", ()=> playerReader.Debuffs.SerpentSting },
                };
            }

            if (BuffDictionary.Keys.Contains(requirement))
            {
                return new Requirement
                {
                    HasRequirement = BuffDictionary[requirement],
                    LogMessage = () => $"{requirementText}"
                };
            }

            if (requirement.StartsWith("not "))
            {
                requirement = requirement.Substring(4);
            }

            if (requirement.StartsWith("!"))
            {
                requirement = requirement.Substring(1);
            }

            if (BuffDictionary.Keys.Contains(requirement))
            {
                return new Requirement
                {
                    HasRequirement = () => !BuffDictionary[requirement](),
                    LogMessage = () => $"{requirementText}"
                };
            }

            logger.LogInformation($"[{name}] UNKNOWN REQUIREMENT! {requirement}: try one of: {string.Join(", ", BuffDictionary.Keys)}");
            return new Requirement
            {
                HasRequirement = () => false,
                LogMessage = () => $"UNKNOWN REQUIREMENT! {requirementText}"
            };
        }

        private Requirement CreateTargetCastingSpellRequirement(string requirement)
        {
            if (requirement.Contains(":"))
            {
                var parts = requirement.Split(":");
                var spellsPart = parts[1].Split("|");
                var spellIds = spellsPart.Select(x => long.Parse(x.Trim())).ToArray();

                var spellIdsStringFormatted = string.Join(", ", spellIds);

                if (requirement.StartsWith("!") || requirement.StartsWith("not "))
                {
                    return new Requirement
                    {
                        HasRequirement = () => !spellIds.Contains(this.playerReader.SpellBeingCastByTarget),
                        LogMessage = () =>
                            $"not Target casting {this.playerReader.SpellBeingCastByTarget} ∈ [{spellIdsStringFormatted}]"
                    };
                }

                return new Requirement
                {
                    HasRequirement = () => spellIds.Contains(this.playerReader.SpellBeingCastByTarget),
                    LogMessage = () =>
                        $"Target casting {this.playerReader.SpellBeingCastByTarget} ∈ [{spellIdsStringFormatted}]"
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

            if (requirement.StartsWith("!") || requirement.StartsWith("not "))
            {
                return new Requirement
                {
                    HasRequirement = () => playerReader.Form != form,
                    LogMessage = () => $"not {form}"
                };
            }
            return new Requirement
            {
                HasRequirement = () => playerReader.Form == form,
                LogMessage = () => $"{playerReader.Form}"
            };
        }

        private Requirement CreateNpcRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var npcId = int.Parse(parts[1]);

            if (requirement.StartsWith("!") || requirement.StartsWith("not "))
            {
                return new Requirement
                {
                    HasRequirement = () => this.playerReader.TargetId != npcId,
                    LogMessage = () => $"not Target id {this.playerReader.TargetId} = {npcId}"
                };
            }
            return new Requirement
            {
                HasRequirement = () => this.playerReader.TargetId == npcId,
                LogMessage = () => $"Target id {this.playerReader.TargetId} = {npcId}"
            };
        }

        private Requirement CreateBagItemRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var itemId = int.Parse(parts[1]);
            var count = parts.Length < 3 ? 1 : int.Parse(parts[2]);

            if (requirement.StartsWith("!") || requirement.StartsWith("not "))
            {
                return new Requirement
                {
                    HasRequirement = () => this.bagReader.ItemCount(itemId) < count,
                    LogMessage = () => count == 1 ? $"{itemId} not in bag" : $"{itemId} count < {count}"
                };
            }
            return new Requirement
            {
                HasRequirement = () => this.bagReader.ItemCount(itemId) >= count,
                LogMessage = () => count == 1 ? $"{itemId} in bag" : $"{itemId} count >= {count}"
            };
        }

        private Requirement CreateSpellInRangeRequirement(string requirement)
        {
            var parts = requirement.Split(":");
            var bitId = int.Parse(parts[1]);

            if (requirement.StartsWith("!") || requirement.StartsWith("not "))
            {
                return new Requirement
                {
                    HasRequirement = () => !this.playerReader.SpellInRange.IsBitSet(bitId),
                    LogMessage = () => $"Not Spell In Range {bitId}"
                };
            }

            return new Requirement
            {
                HasRequirement = () => this.playerReader.SpellInRange.IsBitSet(bitId),
                LogMessage = () => $"Spell In Range {bitId}"
            };
        }

        private Requirement GetValueBasedRequirement(string name, string requirement, Form form)
        {
            var symbol = "<";
            if (requirement.Contains(">"))
            {
                symbol = ">";
            }

            var parts = requirement.Split(symbol);
            var value = int.Parse(parts[1]);

            var valueDictionary = new Dictionary<string, Func<long>>
            {
                {  "Health%", ()=> playerReader.HealthPercent },
                {  "TargetHealth%", ()=> playerReader.TargetHealthPercentage },
                {  "PetHealth%", ()=> playerReader.PetHealthPercentage },
                {  "Mana%", ()=> playerReader.ManaPercentage },
                {  "BagCount", ()=> bagReader.BagItems.Count },
                {  "MobCount", ()=> playerReader.CombatCreatureCount },
                {  "MinRange", ()=> playerReader.MinRange },
                {  "MaxRange", ()=> playerReader.MaxRange },
                {  "LastAutoShotMs", () => playerReader.AutoShot.ElapsedMs },
                {  "LastMainHandMs", () => playerReader.MainHandSwing.ElapsedMs }
            };

            if (!valueDictionary.Keys.Contains(parts[0]))
            {
                logger.LogInformation($"[{name}] UNKNOWN REQUIREMENT! {requirement}: try one of: {string.Join(", ", valueDictionary.Keys)}");
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

    }
}