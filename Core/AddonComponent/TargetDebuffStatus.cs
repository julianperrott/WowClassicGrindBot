namespace Core
{
    public class TargetDebuffStatus : BitStatus
    {
        public TargetDebuffStatus(int value) : base(value)
        {
        }

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
        public bool JudgementoftheCrusader => IsBitSet(0);
        public bool HammerOfJustice => IsBitSet(1);

        // Mage
        public bool Frostbite => IsBitSet(0);
        public bool Slow => IsBitSet(1);

        // Rogue

        // Warrior
        public bool Rend => IsBitSet(0);
        public bool ThunderClap => IsBitSet(1);
        public bool Hamstring => IsBitSet(2);

        // Warlock
        public bool Curseof => IsBitSet(0);
        public bool Corruption => IsBitSet(1);
        public bool Immolate => IsBitSet(2);
        public bool SiphonLife => IsBitSet(3);

        // Hunter
        public bool SerpentSting => IsBitSet(0);
    }
}