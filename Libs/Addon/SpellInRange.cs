namespace Libs
{
    public class SpellInRange
    {
        public readonly long value;

        public SpellInRange(long value)
        {
            this.value = value;
        }

        public bool IsBitSet(int pos)
        {
            return (value & (1 << pos)) != 0;
        }

        // Warrior
        public bool Warrior_Charge { get => IsBitSet(0); }

        public bool Warrior_Rend { get => IsBitSet(1); }
        public bool Warrior_ShootGun { get => IsBitSet(2); }

        // Rogue
        public bool Rogue_SinisterStrike { get => IsBitSet(0); }

        public bool Rogue_Throw { get => IsBitSet(1); }
        public bool Rogue_ShootGun { get => IsBitSet(2); }

        // Priest
        public bool Priest_ShadowWordPain { get => IsBitSet(0); }

        public bool Priest_MindBlast { get => IsBitSet(1); }
        public bool Priest_MindFlay { get => IsBitSet(2); }
        public bool Priest_Shoot { get => IsBitSet(3); }

        // Druid
        public bool Druid_Wrath { get => IsBitSet(0); }

        public bool Druid_Bash { get => IsBitSet(1); }

        //Paladin
        public bool Paladin_Judgement { get => IsBitSet(0); }

        //Mage
        public bool Mage_Fireball { get => IsBitSet(0); }
        public bool Mage_Shoot { get => IsBitSet(1); }
        public bool Mage_Pyroblast { get => IsBitSet(2); }

        //Hunter
        public bool Hunter_RaptorStrike { get => IsBitSet(0); }

        public bool Hunter_ShootGun { get => IsBitSet(1); }

        // Warlock
        public bool Warlock_ShadowBolt { get => IsBitSet(0); }

        public bool Warlock_Shoot { get => IsBitSet(1); }

        public bool WithInPullRange(PlayerClassEnum playerClass) => playerClass switch
        {
            PlayerClassEnum.Warrior => Warrior_Charge,
            PlayerClassEnum.Rogue => Rogue_Throw,
            PlayerClassEnum.Priest => Priest_ShadowWordPain,
            PlayerClassEnum.Druid => Druid_Wrath,
            PlayerClassEnum.Mage => Mage_Pyroblast,
            PlayerClassEnum.Hunter => Hunter_ShootGun,
            PlayerClassEnum.Warlock => Warlock_ShadowBolt,
            _ => true
        };

        public bool WithInCombatRange(PlayerClassEnum playerClass) => playerClass switch
        {
            PlayerClassEnum.Warrior => Warrior_Rend,
            PlayerClassEnum.Rogue => Rogue_SinisterStrike,
            PlayerClassEnum.Priest => Priest_Shoot,
            PlayerClassEnum.Druid => Druid_Bash,
            PlayerClassEnum.Paladin => Paladin_Judgement,
            PlayerClassEnum.Mage => Mage_Fireball,
            PlayerClassEnum.Hunter => Hunter_ShootGun || Hunter_RaptorStrike,
            PlayerClassEnum.Warlock => Warlock_ShadowBolt,
            _ => true
        };
    }
}