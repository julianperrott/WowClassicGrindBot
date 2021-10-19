using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class Blacklist: IBlacklist
    {
        private List<string> blacklist = new List<string>();

        private readonly PlayerReader playerReader;
        private readonly ILogger logger;
        private readonly int above;
        private readonly int below;
        private readonly bool checkTargetGivesExp;

        private long LastWarningTargetGuid = 0;

        public Blacklist(PlayerReader playerReader, int above, int below, bool checkTargetGivesExp, List<string> blacklisted, ILogger logger)
        {
            this.playerReader = playerReader;
            this.logger = logger;
            this.above = above;
            this.below = below;

            this.checkTargetGivesExp = checkTargetGivesExp;

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
            else if (playerReader.DamageTaken.Exists(x => x.LastKnownHealthPercent > 0 && x.CreatureId == playerReader.TargetGuid))
            {
                return false;
            }

            if(this.playerReader.PetHasTarget &&
                this.playerReader.TargetGuid == playerReader.PetGuid)
            {
                return true;
            }

            // it is trying to kill me
            if (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
            {
                return false;
            }

            if (!this.playerReader.PlayerBitValues.TargetIsNormal)
            {
                Warn($"Target is not a normal mob {playerReader.TargetGuid} - {playerReader.TargetId}");
                return true; // ignore elites
            }

            if (this.playerReader.PlayerBitValues.IsTagged)
            {
                Warn($"Target is tagged - {playerReader.TargetGuid} - {playerReader.TargetId}");
                return true; // ignore tagged mobs
            }


            if (checkTargetGivesExp)
            {
                return !this.playerReader.TargetYieldXP;
            }
            else
            {
                if (this.playerReader.TargetLevel > this.playerReader.PlayerLevel + above)
                {
                    Warn($"Target is too high a level {playerReader.TargetGuid} - {playerReader.TargetId}");
                    return true; // ignore if current level + 2
                }

                if (this.playerReader.TargetLevel < this.playerReader.PlayerLevel - below)
                {
                    Warn($"Target is too low a level {playerReader.TargetGuid} - {playerReader.TargetId}");
                    return true; // ignore if current level - 7
                }
            }

            var blacklistMatch = blacklist.Where(s => this.playerReader.Target.ToUpper().StartsWith(s)).FirstOrDefault();
            if (!string.IsNullOrEmpty(blacklistMatch))
            {
                Warn($"Target is in the blacklist {this.playerReader.Target} starts with {blacklistMatch}");
                return true;
            }

            return false;
        }

        private void Warn(string message)
        {
            if (this.playerReader.TargetGuid != this.LastWarningTargetGuid)
            {
                logger.LogWarning($"Blacklisted: {message}");
            }
            this.LastWarningTargetGuid = this.playerReader.TargetGuid;
        }
    }
}