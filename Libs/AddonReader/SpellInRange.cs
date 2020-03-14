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

        public bool WithInPullRange(PlayerClassEnum playerClass) => playerClass switch
        {
            PlayerClassEnum.Warrior => Warrior_ShootGun || Warrior_Charge,
            PlayerClassEnum.Rogue => Rogue_Throw,
            _ => false
        };

        public bool WithInMeleeRange(PlayerClassEnum playerClass) => playerClass switch
        {
            PlayerClassEnum.Warrior => Warrior_Rend,
            PlayerClassEnum.Rogue => Rogue_SinisterStrike,
            _ => false
        };
    }
}