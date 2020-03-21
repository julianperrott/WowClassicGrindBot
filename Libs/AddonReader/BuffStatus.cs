using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class BuffStatus
    {
        public long value;

        public BuffStatus(string name)
        {
            this.name = name;
            this.value = 0;
        }

        public BuffStatus(long value)
        {
            this.value = value;
        }

        public bool IsBitSet(int pos)
        {
            return (value & (1 << pos)) != 0;
        }

        public string name { get; set; } = string.Empty;

        // All
        public bool Eating { get => IsBitSet(0); }
        public bool Drinking { get => IsBitSet(1); }
        public bool WellFed { get => IsBitSet(2); }

        // Priest
        public bool Fortitude { get => IsBitSet(3); }
        public bool InnerFire { get => IsBitSet(4); }
        public bool Renew { get => IsBitSet(5); }
        public bool Shield { get => IsBitSet(6); }
        public bool ManaRegeneration { get => IsBitSet(7); }
        public bool DivineSpirit { get => IsBitSet(8); }
    }
}
