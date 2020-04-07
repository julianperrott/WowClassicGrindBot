using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class Blacklist
    {
        private List<string> blacklist = new List<string> { "THORKA", "SHADOW", "DREADM", "DUNEMA", "BONE C", "FLESH", "REDRID", "MOSSHI", "VOIDWA" };

        private readonly PlayerReader playerReader;

        public Blacklist(PlayerReader playerReader)
        {
            this.playerReader = playerReader;
        }

        public bool IsTargetBlacklisted()
        {
            if (!this.playerReader.PlayerBitValues.TargetIsNormal)
            {
                return true; // ignore elites
            }

            if (this.playerReader.TargetLevel > this.playerReader.Level + 1)
            {
                return true; // ignore if current level + 2
            }

            return blacklist.Contains(this.playerReader.Target);
        }
    }
}
