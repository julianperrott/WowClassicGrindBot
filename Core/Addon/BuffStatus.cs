namespace Core
{
    public class BuffStatus
    {
        public long Value { get; private set; }

        public BuffStatus(long value)
        {
            this.Value = value;
        }

        public bool IsBitSet(int pos)
        {
            return (Value & (1 << pos)) != 0;
        }

        // All
        public bool Eating => IsBitSet(0);
        public bool Drinking => IsBitSet(1);
        public bool WellFed => IsBitSet(2);
        public bool ManaRegeneration => IsBitSet(3);

        // Priest
        public bool Fortitude => IsBitSet(10);
        public bool InnerFire => IsBitSet(11);
        public bool Renew => IsBitSet(12);
        public bool Shield => IsBitSet(13);
        public bool DivineSpirit => IsBitSet(14);

        // Druid
        public bool MarkOfTheWild => IsBitSet(10);
        public bool Thorns => IsBitSet(11);
        public bool TigersFury => IsBitSet(12);
        public bool Prowl => IsBitSet(13);

        // Paladin
        public bool Aura => IsBitSet(10);
        public bool Blessing => IsBitSet(11);
        public bool Seal => IsBitSet(12);

        // Mage
        public bool FrostArmor => IsBitSet(10);
        public bool ArcaneIntellect => IsBitSet(11);
        public bool IceBarrier => IsBitSet(12);
        public bool Ward => IsBitSet(13);
        public bool FirePower => IsBitSet(14);
        public bool ManaShield => IsBitSet(15);

        // Rogue
        public bool SliceAndDice => IsBitSet(10);

        // Warrior
        public bool BattleShout => IsBitSet(10);

        // Warlock
        public bool Demon => IsBitSet(10); //Skin and Armor
        public bool SoulLink => IsBitSet(11);
        public bool SoulstoneResurrection => IsBitSet(12);
        public bool ShadowTrance => IsBitSet(13);

        // Shaman
        public bool LightningShield => IsBitSet(10);
        public bool WaterShield => IsBitSet(11);
        public bool ShamanisticFocus => IsBitSet(12);

        // Hunter
        public bool Aspect => IsBitSet(10); //Any Aspect of
        public bool RapidFire => IsBitSet(11);
        public bool QuickShots => IsBitSet(12);
    }
}