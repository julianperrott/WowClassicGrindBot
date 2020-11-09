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

        private long LastWarningTargetGuid = 0;

        public Blacklist(PlayerReader playerReader, int above, int below, List<string> blacklisted, ILogger logger)
        {
            this.playerReader = playerReader;
            this.logger = logger;
            this.above = above;
            this.below = below;

            blacklisted.ForEach(npc => blacklist.Add(npc.ToUpper()));
        }

        public void Add(string name)
        {
            if (!blacklist.Contains(name))
            {
                blacklist.Add(name);
            }
        }

        public bool IsTargetBlacklisted()
        {
            if (!this.playerReader.HasTarget)
            {
                LastWarningTargetGuid = 0;
                return false;
            }

            // it is trying to kill me
            if (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
            {
                return false;
            }

            if (!this.playerReader.PlayerBitValues.TargetIsNormal)
            {
                Warn("Blacklisted: Target is not a normal mob");
                return true; // ignore elites
            }

            if (this.playerReader.PlayerBitValues.IsTagged)
            {
                Warn("Blacklisted: Target is tagged");
                return true; // ignore tagged mobs
            }

            if (this.playerReader.TargetLevel > this.playerReader.PlayerLevel + above)
            {
                Warn("Blacklisted: Target is too high a level");
                return true; // ignore if current level + 2
            }

            if (this.playerReader.TargetLevel < this.playerReader.PlayerLevel - below)
            {
                Warn("Blacklisted: Target is too low a level");
                return true; // ignore if current level - 7
            }

            var blacklistMatch = blacklist.Where(s => this.playerReader.Target.ToUpper().StartsWith(s)).FirstOrDefault();
            if (!string.IsNullOrEmpty(blacklistMatch))
            {
                Warn($"Blacklisted: Target is in the blacklist { this.playerReader.Target} starts with {blacklistMatch}");
                return true;
            }

            return false;
        }

        private void Warn(string message)
        {
            if (this.playerReader.TargetGuid != this.LastWarningTargetGuid)
            {
                logger.LogWarning(message);
            }
            this.LastWarningTargetGuid = this.playerReader.TargetGuid;
        }
    }
}