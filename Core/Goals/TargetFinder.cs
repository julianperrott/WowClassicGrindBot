using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System.Threading;
using System;

namespace Core.Goals
{
    public class TargetFinder
    {
        private readonly ILogger logger;
        private readonly ConfigurableInput input;
        private readonly ClassConfiguration classConfig;
        private readonly Wait wait;
        private readonly PlayerReader playerReader;

        private readonly IBlacklist blacklist;
        private readonly NpcNameTargeting npcNameTargeting;

        public TargetFinder(ILogger logger, ConfigurableInput input, ClassConfiguration classConfig, Wait wait, PlayerReader playerReader, IBlacklist blacklist, NpcNameTargeting npcNameTargeting)
        {
            this.logger = logger;
            this.classConfig = classConfig;
            this.input = input;
            this.wait = wait;
            this.playerReader = playerReader;

            this.blacklist = blacklist;
            this.npcNameTargeting = npcNameTargeting;
        }

        public bool Search(NpcNames target, Func<bool> validTarget, string source, CancellationToken cts)
        {
            if (LookForTarget(target, source, cts))
            {
                if (validTarget() && !blacklist.IsTargetBlacklisted())
                {
                    logger.LogInformation($"{source}: Has target!");
                    return true;
                }
                else
                {
                    if (!cts.IsCancellationRequested)
                    {
                        input.TapClearTarget($"{source}: Target is invalid!");
                        wait.Update(1);
                    }
                }
            }

            return false;
        }

        private bool LookForTarget(NpcNames target, string source, CancellationToken cts)
        {
            if (!cts.IsCancellationRequested)
            {
                npcNameTargeting.ChangeNpcType(target);
                input.TapNearestTarget(source);
                wait.Update(1);
            }

            if (!cts.IsCancellationRequested && !classConfig.KeyboardOnly && !playerReader.HasTarget)
            {
                npcNameTargeting.ChangeNpcType(target);
                if (!cts.IsCancellationRequested && npcNameTargeting.NpcCount > 0)
                {
                    npcNameTargeting.TargetingAndClickNpc(true, cts);
                    wait.Update(1);
                }
            }

            return playerReader.HasTarget;
        }
    }
}
