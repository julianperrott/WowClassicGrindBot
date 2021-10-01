namespace Core
{
    public enum Form
    {
        None = 0,

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

    public enum StanceActionBar
    {
        WarriorBattleStance = 73 - 1,
        WarriorDefensiveStance = 85 - 1,
        WarriorBerserkerStance = 97 - 1,

        DruidCat = 73 - 1,
        DruidProwl = 85 - 1,
        DruidBear = 97 - 1,
        DruidMoonkin = 109 - 1,

        RogueStealth = 73 - 1,

        PriestShadowform = 73 - 1
    }

    public class Stance
    {
        private readonly int value;

        public Stance(long value)
        {
            this.value = (int)value;
        }

        public Form Get(PlayerClassEnum playerClass) => value == 0 ? Form.None : playerClass switch
        {
            PlayerClassEnum.Warrior => Form.Warrior_BattleStance + value - 1,
            PlayerClassEnum.Rogue => Form.Rogue_Stealth + value - 1,
            PlayerClassEnum.Priest => Form.Priest_Shadowform + value - 1,
            PlayerClassEnum.Druid => Form.Druid_Bear + value - 1,
            PlayerClassEnum.Paladin => Form.Paladin_Devotion_Aura + value - 1,
            PlayerClassEnum.Shaman => Form.Shaman_GhostWolf + value - 1,
            _ => Form.None
        };

        public static int MapActionBar(PlayerReader playerReader, int slot)
        {
            if (slot > 12 || playerReader.Form == Form.None)
                return 0;

            switch (playerReader.PlayerClass)
            {
                case PlayerClassEnum.Druid:
                    switch (playerReader.Form)
                    {
                        case Form.Druid_Cat:
                            if (playerReader.Buffs.Prowl)
                                return (int)StanceActionBar.DruidProwl;
                            else
                                return (int)StanceActionBar.DruidCat;
                        case Form.Druid_Bear:
                            return (int)StanceActionBar.DruidBear;
                        case Form.Druid_Moonkin:
                            return (int)StanceActionBar.DruidMoonkin;
                    }
                    break;
                case PlayerClassEnum.Warrior:
                    switch (playerReader.Form)
                    {
                        case Form.Warrior_BattleStance:
                            return (int)StanceActionBar.WarriorBattleStance;
                        case Form.Warrior_DefensiveStance:
                            return (int)StanceActionBar.WarriorDefensiveStance;
                        case Form.Warrior_BerserkerStance:
                            return (int)StanceActionBar.WarriorBerserkerStance;
                    }
                    break;
                case PlayerClassEnum.Rogue:
                    if (playerReader.Form == Form.Rogue_Stealth)
                        return (int)StanceActionBar.RogueStealth;
                    break;
                case PlayerClassEnum.Priest:
                    if (playerReader.Form == Form.Priest_Shadowform)
                        return (int)StanceActionBar.PriestShadowform;
                    break;
            }

            return 0;
        }
    }
}
