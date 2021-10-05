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
        Druid_Cat_Prowl = 7,

        Priest_Shadowform = 8,

        Rogue_Stealth = 9,
        Rogue_Vanish = 10,

        Shaman_GhostWolf = 11,

        Warrior_BattleStance = 12,
        Warrior_DefensiveStance = 13,
        Warrior_BerserkerStance = 14,

        Paladin_Devotion_Aura = 15,
        Paladin_Retribution_Aura = 16,
        Paladin_Concentration_Aura = 17,
        Paladin_Shadow_Resistance_Aura = 18,
        Paladin_Frost_Resistance_Aura = 19,
        Paladin_Fire_Resistance_Aura = 20,
        Paladin_Crusader_Aura = 21,
    }

    public enum StanceActionBar
    {
        WarriorBattleStance = 73 - 1,
        WarriorDefensiveStance = 85 - 1,
        WarriorBerserkerStance = 97 - 1,

        DruidCat = 73 - 1,
        DruidCatProwl = 85 - 1,
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

        public Form Get(PlayerReader playerReader, PlayerClassEnum playerClass) => value == 0 ? Form.None : playerClass switch
        {
            PlayerClassEnum.Warrior => Form.Warrior_BattleStance + value - 1,
            PlayerClassEnum.Rogue => Form.Rogue_Stealth + value - 1,
            PlayerClassEnum.Priest => Form.Priest_Shadowform + value - 1,
            PlayerClassEnum.Druid => playerReader.Buffs.Prowl ? Form.Druid_Cat_Prowl + value - 1 : Form.Druid_Bear + value - 1,
            PlayerClassEnum.Paladin => Form.Paladin_Devotion_Aura + value - 1,
            PlayerClassEnum.Shaman => Form.Shaman_GhostWolf + value - 1,
            _ => Form.None
        };

        public static int RuntimeSlotToActionBar(PlayerReader playerReader, int slot)
        {
            if (slot > 12 || playerReader.Form == Form.None)
                return 0;

            return FormToActionBar(playerReader.PlayerClass, playerReader.Form);
        }

        public static int FormToActionBar(PlayerClassEnum playerClass, Form form)
        {
            switch (playerClass)
            {
                case PlayerClassEnum.Druid:
                    switch (form)
                    {
                        case Form.Druid_Cat:
                            return (int)StanceActionBar.DruidCat;
                        case Form.Druid_Cat_Prowl:
                            return (int)StanceActionBar.DruidCatProwl;
                        case Form.Druid_Bear:
                            return (int)StanceActionBar.DruidBear;
                        case Form.Druid_Moonkin:
                            return (int)StanceActionBar.DruidMoonkin;
                    }
                    break;
                case PlayerClassEnum.Warrior:
                    switch (form)
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
                    if (form == Form.Rogue_Stealth)
                        return (int)StanceActionBar.RogueStealth;
                    break;
                case PlayerClassEnum.Priest:
                    if (form == Form.Priest_Shadowform)
                        return (int)StanceActionBar.PriestShadowform;
                    break;
            }
            return 0;
        }
    }
}
