namespace Core
{
    public class BuffStatus : BitStatus
    {
        public BuffStatus(int value) : base(value)
        {
        }

        // All
        public bool Eating => IsBitSet(0);
        public bool Drinking => IsBitSet(1);
        public bool WellFed => IsBitSet(2);
        public bool ManaRegeneration => IsBitSet(3);
        public bool Clearcasting => IsBitSet(4);

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
        public bool Rejuvenation => IsBitSet(14);
        public bool Regrowth => IsBitSet(15);

        // Paladin
        public bool SealofRighteousness => IsBitSet(5);
        public bool SealoftheCrusader => IsBitSet(6);
        public bool SealofCommand => IsBitSet(7);
        public bool SealofWisdom => IsBitSet(8);
        public bool SealofLight => IsBitSet(9);
        public bool SealofBlood => IsBitSet(10);
        public bool SealofVengeance => IsBitSet(11);

        public bool BlessingofMight => IsBitSet(12);
        public bool BlessingofProtection => IsBitSet(13);
        public bool BlessingofWisdom => IsBitSet(14);
        public bool BlessingofKings => IsBitSet(15);
        public bool BlessingofSalvation => IsBitSet(16);
        public bool BlessingofSanctuary => IsBitSet(17);
        public bool BlessingofLight => IsBitSet(18);

        public bool RighteousFury => IsBitSet(19);
        public bool DivineProtection => IsBitSet(20);
        public bool AvengingWrath => IsBitSet(21);
        public bool HolyShield => IsBitSet(22);

        // Mage
        public bool FrostArmor => IsBitSet(10);
        public bool ArcaneIntellect => IsBitSet(11);
        public bool IceBarrier => IsBitSet(12);
        public bool Ward => IsBitSet(13);
        public bool FirePower => IsBitSet(14);
        public bool ManaShield => IsBitSet(15);
        public bool PresenceOfMind => IsBitSet(16);
        public bool ArcanePower => IsBitSet(17);

        // Rogue
        public bool SliceAndDice => IsBitSet(10);
        public bool Stealth => IsBitSet(11);

        // Warrior
        public bool BattleShout => IsBitSet(10);
        public bool Bloodrage => IsBitSet(11);

        // Warlock
        public bool Demon => IsBitSet(10); //Skin and Armor
        public bool SoulLink => IsBitSet(11);
        public bool SoulstoneResurrection => IsBitSet(12);
        public bool ShadowTrance => IsBitSet(13);

        // Shaman
        public bool LightningShield => IsBitSet(10);
        public bool WaterShield => IsBitSet(11);
        public bool ShamanisticFocus => IsBitSet(12);
        public bool Stoneskin => IsBitSet(13);

        // Hunter
        public bool Aspect => IsBitSet(10); //Any Aspect of
        public bool RapidFire => IsBitSet(11);
        public bool QuickShots => IsBitSet(12);
    }
}