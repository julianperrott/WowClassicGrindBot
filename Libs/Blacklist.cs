using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class Blacklist
    {
        private List<string> blacklist = new List<string> { "THORKA", "SHADOW", "DREADM", "DUNEMA", "BONE C", "FLESH", "REDRID", "MOSSHI", "VOIDWA","WAILIN" };

        private readonly PlayerReader playerReader;

        public Blacklist(PlayerReader playerReader)
        {
            this.playerReader = playerReader;
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

            if (this.playerReader.TargetLevel > this.playerReader.PlayerLevel + 1)
            {
                return true; // ignore if current level + 2
            }

            if (this.playerReader.TargetLevel < this.playerReader.PlayerLevel - 7)
            {
                return true; // ignore if current level - 7
            }

            return blacklist.Contains(this.playerReader.Target);
        }
    }
}
