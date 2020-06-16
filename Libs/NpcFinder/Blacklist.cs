using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Libs
{
    public class Blacklist: IBlacklist
    {
        private List<string> blacklist = new List<string> { "THORKA", "SHADOW", "DREADM", "DUNEMA", "BONE C", "FLESH", "REDRID", "MOSSHI", "VOIDWA", "WAILIN" };

        private readonly PlayerReader playerReader;
        private readonly ILogger logger;
        private readonly int above;
        private readonly int below;

        public Blacklist(PlayerReader playerReader, int above, int below, List<string> blacklisted, ILogger logger)
        {
            this.playerReader = playerReader;
            this.logger = logger;
            this.above = above;
            this.below = below;

            blacklisted.ForEach(npc => blacklist.Add(npc.ToUpper()));
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
                logger.LogWarning("Blacklisted: Target is not a normal mob");
                return true; // ignore elites
            }

            if (this.playerReader.PlayerBitValues.IsTagged)
            {
                logger.LogWarning("Blacklisted: Target is tagged");
                return true; // ignore tagged mobs
            }

            if (this.playerReader.TargetLevel > this.playerReader.PlayerLevel + above)
            {
                logger.LogWarning("Blacklisted: Target is too high a level");
                return true; // ignore if current level + 2
            }

            if (this.playerReader.TargetLevel < this.playerReader.PlayerLevel - below)
            {
                logger.LogWarning("Blacklisted: Target is too low a level");
                return true; // ignore if current level - 7
            }

            var blacklistMatch = blacklist.Where(s => this.playerReader.Target.ToUpper().StartsWith(s)).FirstOrDefault();
            if (!string.IsNullOrEmpty(blacklistMatch))
            {
                logger.LogWarning($"Blacklisted: Target is in the blacklist { this.playerReader.Target} starts with {blacklistMatch}");
                return true;
            }

            return false;
        }
    }
}