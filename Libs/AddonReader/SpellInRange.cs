using System;
using System.Collections.Generic;
using System.Text;

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

        public bool Charge { get => IsBitSet(0); }
        public bool Rend { get => IsBitSet(1); }
        public bool ShootGun { get => IsBitSet(2); }
    }
}
