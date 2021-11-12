using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class Blacklist : IBlacklist
    {
        private readonly List<string> blacklist = new List<string>();

        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly ILogger logger;
        private readonly int above;
        private readonly int below;
        private readonly bool checkTargetGivesExp;

        private int LastWarningTargetGuid = 0;

        public Blacklist(ILogger logger, AddonReader addonReader, int above, int below, bool checkTargetGivesExp, List<string> blacklisted)
        {
            this.addonReader = addonReader;
            playerReader = addonReader.PlayerReader;
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
            if (!playerReader.HasTarget)
            {
                LastWarningTargetGuid = 0;
                return false;
            }
            else if (addonReader.CreatureHistory.DamageTaken.Exists(x => x.HealthPercent > 0 && x.Guid == playerReader.TargetGuid))
            {
                return false;
            }

            if (playerReader.PetHasTarget && playerReader.TargetGuid == playerReader.PetGuid)
            {
                return true;
            }

            // it is trying to kill me
            if (playerReader.Bits.TargetOfTargetIsPlayer)
            {
                return false;
            }

            if (!playerReader.Bits.TargetIsNormal)
            {
                Warn($"Target is not a normal mob {playerReader.TargetGuid} - {playerReader.TargetId}");
                return true; // ignore elites
            }

            if (playerReader.Bits.IsTagged)
            {
                Warn($"Target is tagged - {playerReader.TargetGuid} - {playerReader.TargetId}");
                return true; // ignore tagged mobs
            }


            if (playerReader.TargetLevel > playerReader.Level.Value + above)
            {
                Warn($"Target is too high a level {playerReader.TargetGuid} - {playerReader.TargetId}");
                return true; // ignore if current level + 2
            }

            if (checkTargetGivesExp)
            {
                if (!playerReader.TargetYieldXP)
                {
                    Warn($"Target is not yield experience {playerReader.TargetGuid} - {playerReader.TargetId}");
                    return true;
                }
            }
            else if (playerReader.TargetLevel < playerReader.Level.Value - below)
            {
                Warn($"Target is too low a level {playerReader.TargetGuid} - {playerReader.TargetId}");
                return true; // ignore if current level - 7
            }

            string blacklistMatch = blacklist.FirstOrDefault(s => addonReader.TargetName.ToUpper().StartsWith(s));
            if (!string.IsNullOrEmpty(blacklistMatch))
            {
                Warn($"Target is in the blacklist {addonReader.TargetName} starts with {blacklistMatch}");
                return true;
            }

            return false;
        }

        private void Warn(string message)
        {
            if (playerReader.TargetGuid != LastWarningTargetGuid)
            {
                logger.LogWarning($"Blacklisted: {message}");
            }
            LastWarningTargetGuid = playerReader.TargetGuid;
        }
    }
}