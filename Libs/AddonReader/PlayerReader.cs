using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class PlayerReader
    {
        private readonly ISquareReader reader;
        private readonly List<DataFrame> f;

        public PlayerReader(ISquareReader reader, List<DataFrame> frames)
        {
            this.reader = reader;
            this.f = frames;
        }

        public double XCoord => reader.GetFixedPointAtCell(f[1]) * 10;
        public double YCoord => reader.GetFixedPointAtCell(f[2]) * 10;
        public double Direction => reader.GetFixedPointAtCell(f[3]);

        public string Zone => reader.GetStringAtCell(f[4]) + reader.GetStringAtCell(f[5]); // Checks current geographic zone

        // gets the position of your corpse where you died
        public double CorpseX => reader.GetFixedPointAtCell(f[6]);
        public double CorpseY => reader.GetFixedPointAtCell(f[7]);


        public PlayerBitValues PlayerBitValues => new PlayerBitValues(reader.GetLongAtCell(f[8]));

        // Player health and mana
        public long HealthMax => reader.GetLongAtCell(f[10]); // Maximum amount of health of player
        public long HealthCurrent => reader.GetLongAtCell(f[11]); // Current amount of health of player
        public long HealthPercent => (HealthCurrent * 100) / HealthMax; // Health in terms of a percentage

        public long ManaMax => reader.GetLongAtCell(f[12]); // Maximum amount of mana
        public long ManaCurrent => reader.GetLongAtCell(f[13]); // Current amount of mana
        public long Mana => (ManaCurrent * 100) / ManaMax; // Mana in terms of a percentage
                                                           // Level is our character's exact level ranging from 1-60
        public long Level => reader.GetLongAtCell(f[14]);

        // Todo !
        // range detects if a target range. Bases information off of action slot 2, 3, and 4. Outputs: 50, 35, 30, or 20
        public long Range => reader.GetLongAtCell(f[15]);

        // target bane
        public string Target => reader.GetStringAtCell(f[16]) + (reader.GetStringAtCell(f[17]));

        public long TargetMaxHealth => reader.GetLongAtCell(f[18]);

        // Targets current percentage of health
        public long TargetHealth => reader.GetLongAtCell(f[19]);


        // 32 - 33
        public long Gold => reader.GetLongAtCell(f[32]) + reader.GetLongAtCell(f[33]) * 1000000;

        // 34 -36 Loops through binaries of three pixels. Currently does 24 slots. 1-12 and 61-72.
        public ActionBarStatus ActionBarUseable_1To24 => new ActionBarStatus(reader.GetLongAtCell(f[34]));
        public ActionBarStatus ActionBarUseable_25To48 => new ActionBarStatus(reader.GetLongAtCell(f[35]));
        public ActionBarStatus ActionBarUseable_49To72 => new ActionBarStatus(reader.GetLongAtCell(f[36]));
        public ActionBarStatus ActionBarUseable_73To96 => new ActionBarStatus(reader.GetLongAtCell(f[42]));

        // 37- 40 Bag Slots 
        public long BagSlot1Slots => reader.GetLongAtCell(f[37]);
        public long BagSlot2Slots => reader.GetLongAtCell(f[38]);
        public long BagSlot3Slots => reader.GetLongAtCell(f[39]);
        public long BagSlot4Slots => reader.GetLongAtCell(f[40]);


        public long SkinningLevel => reader.GetLongAtCell(f[41]);
        public long FishingLevel => reader.GetLongAtCell(f[42]);


        //MakePixelSquareArr(integerToColor(self: GetDebuffs("FrostNova")), 43)-- Checks if target is frozen by frost nova debuff

        public long Gametime => reader.GetLongAtCell(f[44]);// Returns time in the game
        public long GossipOptions => reader.GetLongAtCell(f[45]); //  Returns which gossip icons are on display in dialogue box
        public long Playerclass => reader.GetLongAtCell(f[46]); // Returns player class as an integer
        public bool Unskinnable => reader.GetLongAtCell(f[47]) != 0; // Returns 1 if creature is unskinnable
        public long HearthZone => reader.GetLongAtCell(f[48]); // Returns subzone of that is currently bound to hearthstone

        public SpellInRange SpellInRange => new SpellInRange(reader.GetLongAtCell(f[49]));

        public bool WithInPullRange => SpellInRange.ShootGun || SpellInRange.Charge;
        public bool WithInMeleeRange => SpellInRange.Rend;
    }
}
