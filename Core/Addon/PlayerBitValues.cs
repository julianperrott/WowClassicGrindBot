namespace Core
{
    public class PlayerBitValues
    {
        private readonly long value;

        public PlayerBitValues(long value)
        {
            this.value = value;
        }

        public bool IsBitSet(int pos)
        {
            return (value & (1 << pos)) != 0;
        }

        public bool TargetInCombat => IsBitSet(0);
        public bool TargetIsDead => IsBitSet(1);
        public bool DeadStatus => IsBitSet(2);
        public bool TalentPoints => IsBitSet(3);
        public bool IsInDeadZoneRange => IsBitSet(4);
        public bool TargetCanBeHostile => IsBitSet(5);
        public bool HasPet => IsBitSet(6);
        public bool MainHandEnchant_Active => IsBitSet(7);
        public bool OffHandEnchant_Active => IsBitSet(8);
        public bool ItemsAreBroken => IsBitSet(9);
        public bool IsFlying => IsBitSet(10);
        public bool IsSwimming => IsBitSet(11);
        public bool PetHappy => IsBitSet(12);
        public bool HasAmmo => IsBitSet(13);
        public bool PlayerInCombat => IsBitSet(14);
        public bool TargetOfTargetIsPlayer => IsBitSet(15);
        public bool IsAutoRepeatSpellOn_AutoShot => IsBitSet(16);
        public bool HasTarget => IsBitSet(17);
        public bool IsMounted => IsBitSet(18);
        public bool IsAutoRepeatSpellOn_Shoot => IsBitSet(19);
        public bool IsAutoRepeatSpellOn_AutoAttack => IsBitSet(20);
        public bool TargetIsNormal => IsBitSet(21);
        public bool IsTagged => IsBitSet(22);
        public bool IsFalling => IsBitSet(23);
    }
}