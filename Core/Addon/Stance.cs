using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    public enum Form
    {
        Humanoid = 0,

        Druid_Bear = 1,
        Druid_Aquatic = 2,
        Druid_Cat = 3,
        Druid_Travel = 4,
        Druid_Moonkin = 5,
        Druid_Flight = 6,

        Priest_Shadowform = 7,

        Rogue_Stealth = 8,
        Rogue_Vanish = 9,

        Shaman_GhostWolf = 10,

        Warrior_BattleStance = 11,
        Warrior_DefensiveStance = 12,
        Warrior_BerserkerStance = 13,

        Paladin_Devotion_Aura = 14,
        Paladin_Retribution_Aura = 15,
        Paladin_Concentration_Aura = 16,
        Paladin_Shadow_Resistance_Aura = 17,
        Paladin_Frost_Resistance_Aura = 18,
        Paladin_Fire_Resistance_Aura = 19,
        Paladin_Crusader_Aura = 20,
    }

    public class Stance
    {
        private readonly int value;

        public Stance(long value)
        {
            this.value = (int)value;
        }

        public Form Get(PlayerClassEnum playerClass) => value == 0 ? Form.Humanoid : playerClass switch
        {
            PlayerClassEnum.Warrior => Form.Warrior_BattleStance + value - 1,
            PlayerClassEnum.Rogue => Form.Rogue_Stealth + value - 1,
            PlayerClassEnum.Priest => Form.Priest_Shadowform + value - 1,
            PlayerClassEnum.Druid => Form.Druid_Bear + value - 1,
            PlayerClassEnum.Paladin => Form.Paladin_Devotion_Aura + value - 1,
            PlayerClassEnum.Shaman => Form.Shaman_GhostWolf + value - 1,
            _ => Form.Humanoid
        };
    }
}
