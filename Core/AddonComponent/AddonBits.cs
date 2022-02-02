namespace Core
{
    public class AddonBits
    {
        private readonly BitStatus v1;
        private readonly BitStatus v2;

        public AddonBits(int value1, int value2)
        {
            v1 = new BitStatus(value1);
            v2 = new BitStatus(value2);
        }

        // -- value1 based flags
        public bool TargetInCombat => v1.IsBitSet(0);
        public bool TargetIsDead => v1.IsBitSet(1);
        public bool DeadStatus => v1.IsBitSet(2);
        public bool TalentPoints => v1.IsBitSet(3);
        public bool IsInDeadZoneRange => v1.IsBitSet(4);
        public bool TargetCanBeHostile => v1.IsBitSet(5);
        public bool HasPet => v1.IsBitSet(6);
        public bool MainHandEnchant_Active => v1.IsBitSet(7);
        public bool OffHandEnchant_Active => v1.IsBitSet(8);
        public bool ItemsAreBroken => v1.IsBitSet(9);
        public bool IsFlying => v1.IsBitSet(10);
        public bool IsSwimming => v1.IsBitSet(11);
        public bool PetHappy => v1.IsBitSet(12);
        public bool HasAmmo => v1.IsBitSet(13);
        public bool PlayerInCombat => v1.IsBitSet(14);
        public bool TargetOfTargetIsPlayer => v1.IsBitSet(15);
        public bool IsAutoRepeatSpellOn_AutoShot => v1.IsBitSet(16);
        public bool HasTarget => v1.IsBitSet(17);
        public bool IsMounted => v1.IsBitSet(18);
        public bool IsAutoRepeatSpellOn_Shoot => v1.IsBitSet(19);
        public bool IsAutoRepeatSpellOn_AutoAttack => v1.IsBitSet(20);
        public bool TargetIsNormal => v1.IsBitSet(21);
        public bool IsTagged => v1.IsBitSet(22);
        public bool IsFalling => v1.IsBitSet(23);

        // -- value2 based flags
        public bool IsDrowning => v2.IsBitSet(0);

        public bool IsCorpseInRange => v2.IsBitSet(1);
    }
}