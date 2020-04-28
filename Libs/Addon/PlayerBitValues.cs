using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class PlayerBitValues
    {
        public long value;
        public PlayerBitValues(long value)
        {
            this.value = value;
        }

        public bool IsBitSet(int pos)
        {
            return (value & (1 << pos)) != 0;
        }

        public bool IsTagged { get => IsBitSet(22); }
        public bool TargetIsNormal { get => IsBitSet(21); }
        public bool IsAutoRepeatSpellOn_Shoot { get => IsBitSet(19); }
        public bool IsMounted { get => IsBitSet(18); }
        public bool ProcessExitStatus { get => IsBitSet(17); }

        public bool TargetOfTargetIsPlayer { get => IsBitSet(15); }
        public bool PlayerInCombat { get => IsBitSet(14); }
        

        public bool Flying { get => IsBitSet(10); }
        public bool ItemsAreBroken { get => IsBitSet(9); }

        public bool HasPet { get => IsBitSet(6); }
        
    
        public bool TalentPoints { get => IsBitSet(3); }
        public bool DeadStatus { get => IsBitSet(2); }
        public bool TargetIsDead { get => IsBitSet(1); }
        public bool TargetInCombat { get => IsBitSet(0); }
    }
}
