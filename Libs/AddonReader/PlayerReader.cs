using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class PlayerReader
    {
        private readonly ISquareReader reader;

        public PlayerReader(ISquareReader reader)
        {
            this.reader = reader;
        }

        public WowPoint PlayerLocation => new WowPoint(XCoord, YCoord);

        public double XCoord => reader.GetFixedPointAtCell(1) * 100;
        public double YCoord => reader.GetFixedPointAtCell(2) * 100;
        public double Direction => reader.GetFixedPointAtCell(3);

        public string Zone => reader.GetStringAtCell(4) + reader.GetStringAtCell(5); // Checks current geographic zone

        public WowPoint CorpseLocation => new WowPoint(CorpseX, CorpseY);

        // gets the position of your corpse where you died
        public double CorpseX => reader.GetFixedPointAtCell(6)*10;
        public double CorpseY => reader.GetFixedPointAtCell(7) * 10;


        public PlayerBitValues PlayerBitValues => new PlayerBitValues(reader.GetLongAtCell(8));

        // Player health and mana
        public long HealthMax => reader.GetLongAtCell(10); // Maximum amount of health of player
        public long HealthCurrent => reader.GetLongAtCell(11); // Current amount of health of player
        public long HealthPercent => HealthMax==0 ?0 : (HealthCurrent * 100) / HealthMax; // Health in terms of a percentage

        public long ManaMax => reader.GetLongAtCell(12); // Maximum amount of mana
        public long ManaCurrent => reader.GetLongAtCell(13); // Current amount of mana
        public long Mana => (ManaCurrent * 100) / ManaMax; // Mana in terms of a percentage
                                                           // Level is our character's exact level ranging from 1-60
        public long Level => reader.GetLongAtCell(14);

        // Todo !
        // range detects if a target range. Bases information off of action slot 2, 3, and 4. Outputs: 50, 35, 30, or 20
        public long Range => reader.GetLongAtCell(15);

        // target bane
        public string Target => reader.GetStringAtCell(16) + (reader.GetStringAtCell(17));

        public long TargetMaxHealth => reader.GetLongAtCell(18);

        // Targets current percentage of health
        public double TargetHealthPercentage => ((double)TargetHealth*100)/ TargetMaxHealth;

        public long TargetHealth => reader.GetLongAtCell(19);

        public bool HasTarget => !string.IsNullOrEmpty(Target) || TargetHealth > 0;

        // 32 - 33
        public long Gold => reader.GetLongAtCell(32) + reader.GetLongAtCell(33) * 1000000;

        // 34 -36 Loops through binaries of three pixels. Currently does 24 slots. 1-12 and 61-72.
        public ActionBarStatus ActionBarUseable_1To24 => new ActionBarStatus(reader.GetLongAtCell(34));
        public ActionBarStatus ActionBarUseable_25To48 => new ActionBarStatus(reader.GetLongAtCell(35));
        public ActionBarStatus ActionBarUseable_49To72 => new ActionBarStatus(reader.GetLongAtCell(36));
        public ActionBarStatus ActionBarUseable_73To96 => new ActionBarStatus(reader.GetLongAtCell(42));

        // 37- 40 Bag Slots 
        public long BagSlot1Slots => reader.GetLongAtCell(37);
        public long BagSlot2Slots => reader.GetLongAtCell(38);
        public long BagSlot3Slots => reader.GetLongAtCell(39);
        public long BagSlot4Slots => reader.GetLongAtCell(40);


        public long SkinningLevel => reader.GetLongAtCell(41);
        public long FishingLevel => reader.GetLongAtCell(42);


        //MakePixelSquareArr(integerToColor(self: GetDebuffs("FrostNova")), 43)-- Checks if target is frozen by frost nova debuff

        public long Gametime => reader.GetLongAtCell(44);// Returns time in the game
        public long GossipOptions => reader.GetLongAtCell(45); //  Returns which gossip icons are on display in dialogue box
        public long Playerclass => reader.GetLongAtCell(46); // Returns player class as an integer
        public bool Unskinnable => reader.GetLongAtCell(47) != 0; // Returns 1 if creature is unskinnable
        public long HearthZone => reader.GetLongAtCell(48); // Returns subzone of that is currently bound to hearthstone

        public SpellInRange SpellInRange => new SpellInRange(reader.GetLongAtCell(49));

        public bool WithInPullRange => SpellInRange.ShootGun || SpellInRange.Charge;
        public bool WithInMeleeRange => SpellInRange.Rend;
    }
}
