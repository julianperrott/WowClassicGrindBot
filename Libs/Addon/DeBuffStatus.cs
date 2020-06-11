namespace Libs
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
        public bool ShadowWordPain { get => IsBitSet(0); }

        // Druid
        public bool Roar { get => IsBitSet(0); }

        public bool FaerieFire { get => IsBitSet(1); }

        // Paladin

        // Mage

        // Rogue

        // Warrior

        // Warlock
        public bool CurseofWeakness { get => IsBitSet(0); }

        public bool Frostbite { get => IsBitSet(0); }
    }
}