namespace Core
{
    public class PlayerBitValues
    {
        private readonly long value1;
        private readonly long value2;

        public PlayerBitValues(long value1, long value2)
        {
            this.value1 = value1;
            this.value2 = value2;
        }

        private bool IsBitSet1(int pos)
        {
            return (value1 & (1 << pos)) != 0;
        }
        private bool IsBitSet2(int pos)
        {
            return (value2 & (1 << pos)) != 0;
        }


        // -- value1 based flags
        public bool TargetInCombat => IsBitSet1(0);
        public bool TargetIsDead => IsBitSet1(1);
        public bool DeadStatus => IsBitSet1(2);
        public bool TalentPoints => IsBitSet1(3);
        public bool IsInDeadZoneRange => IsBitSet1(4);
        public bool TargetCanBeHostile => IsBitSet1(5);
        public bool HasPet => IsBitSet1(6);
        public bool MainHandEnchant_Active => IsBitSet1(7);
        public bool OffHandEnchant_Active => IsBitSet1(8);
        public bool ItemsAreBroken => IsBitSet1(9);
        public bool IsFlying => IsBitSet1(10);
        public bool IsSwimming => IsBitSet1(11);
        public bool PetHappy => IsBitSet1(12);
        public bool HasAmmo => IsBitSet1(13);
        public bool PlayerInCombat => IsBitSet1(14);
        public bool TargetOfTargetIsPlayer => IsBitSet1(15);
        public bool IsAutoRepeatSpellOn_AutoShot => IsBitSet1(16);
        public bool HasTarget => IsBitSet1(17);
        public bool IsMounted => IsBitSet1(18);
        public bool IsAutoRepeatSpellOn_Shoot => IsBitSet1(19);
        public bool IsAutoRepeatSpellOn_AutoAttack => IsBitSet1(20);
        public bool TargetIsNormal => IsBitSet1(21);
        public bool IsTagged => IsBitSet1(22);
        public bool IsFalling => IsBitSet1(23);

        // -- value2 based flags
        public bool IsDrowning => IsBitSet2(0);
    }
}