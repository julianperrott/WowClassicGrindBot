using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class Blacklist
    {
        private List<string> blacklist = new List<string> { "THORKA", "SHADOW", "DREADM", "DUNEMA", "BONE C", "FLESH", "REDRID", "MOSSHI", "VOIDWA","WAILIN" };

        private readonly PlayerReader playerReader;
        private readonly int above;
        private readonly int below;

        public Blacklist(PlayerReader playerReader, int above, int below, List<string> blacklisted)
        {
            this.playerReader = playerReader;
            this.above = above;
            this.below = below;

            blacklisted.ForEach(npc => blacklist.Add(npc.ToUpper().Substring(0, 6)));
        }

        public bool IsTargetBlacklisted()
        {
            if (!this.playerReader.HasTarget)
            {
                return false;
            }

            // it is trying to kill me
            if (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
            {
                return false;
            }

            if (!this.playerReader.PlayerBitValues.TargetIsNormal)
            {
                return true; // ignore elites
            }

            if (this.playerReader.PlayerBitValues.IsTagged)
            {
                return true; // ignore tagged mobs
            }

            if (this.playerReader.TargetLevel > this.playerReader.PlayerLevel + above)
            {
                return true; // ignore if current level + 2
            }

            if (this.playerReader.TargetLevel < this.playerReader.PlayerLevel - below)
            {
                return true; // ignore if current level - 7
            }

            return blacklist.Contains(this.playerReader.Target);
        }
    }
}
