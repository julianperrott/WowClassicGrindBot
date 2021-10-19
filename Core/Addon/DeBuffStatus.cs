namespace Core
{
    public class DebuffStatus
    {
        private long value;

        public DebuffStatus(string name)
        {
            this.name = name;
            this.value = 0;
        }

        public DebuffStatus(long value)
        {
            this.value = value;
        }

        public bool IsBitSet(int pos)
        {
            return (value & (1 << pos)) != 0;
        }

        public string name { get; set; } = string.Empty;

        // Priest
        public bool ShadowWordPain => IsBitSet(0);

        // Druid
        public bool Roar => IsBitSet(0);
        public bool FaerieFire => IsBitSet(1);
        public bool Rip => IsBitSet(2);
        public bool Moonfire => IsBitSet(3);
        public bool EntanglingRoots => IsBitSet(4);
        public bool Rake => IsBitSet(5);

        // Paladin

        // Mage
        public bool Frostbite => IsBitSet(0);
        public bool Slow => IsBitSet(1);

        // Rogue

        // Warrior
        public bool Rend => IsBitSet(0);

        // Warlock
        public bool Curseof => IsBitSet(0);
        public bool Corruption => IsBitSet(1);
        public bool Immolate => IsBitSet(2);
        public bool SiphonLife => IsBitSet(3);

        // Hunter
        public bool SerpentSting => IsBitSet(0);
    }
}