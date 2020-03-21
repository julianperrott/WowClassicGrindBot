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

        public bool WithInPullRange(PlayerClassEnum playerClass) => playerClass switch
        {
            PlayerClassEnum.Warrior => Warrior_ShootGun || Warrior_Charge,
            PlayerClassEnum.Rogue => Rogue_Throw,
            PlayerClassEnum.Priest => Priest_ShadowWordPain,
            _ => false
        };

        public bool WithInCombatRange(PlayerClassEnum playerClass) => playerClass switch
        {
            PlayerClassEnum.Warrior => Warrior_Rend,
            PlayerClassEnum.Rogue => Rogue_SinisterStrike,
            PlayerClassEnum.Priest => Priest_Shoot,
            _ => false
        };
    }
}