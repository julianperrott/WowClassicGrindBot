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

        public bool ProcessExitStatus { get => IsBitSet(17); }
        public bool NeedManaGem { get => IsBitSet(16); }
        public bool TargetOfTargetIsPlayer { get => IsBitSet(15); }
        public bool PlayerInCombat { get => IsBitSet(14); }
        public bool Spell_drinkWater_active { get => IsBitSet(13); }
        public bool Spell_evocation_active { get => IsBitSet(12); }
        public bool NeedFood { get => IsBitSet(11); }
        public bool Flying { get => IsBitSet(10); }
        public bool ItemsAreBroken { get => IsBitSet(9); }
        public bool Spell_iceBarrier_active { get => IsBitSet(8); }
        public bool Spell_arcaneIntellect_active { get => IsBitSet(7); }
        public bool Spell_frostArmor_active { get => IsBitSet(6); }
        public bool Spell_eatFood_active { get => IsBitSet(5); }
        public bool NeedWater { get => IsBitSet(4); }
        public bool TalentPoints { get => IsBitSet(3); }
        public bool DeadStatus { get => IsBitSet(2); }
        public bool TargetIsDead { get => IsBitSet(1); }
        public bool TargetInCombat { get => IsBitSet(0); }
    }
}
