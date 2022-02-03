using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

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

        private readonly Random random = new Random();

        public NpcNames NpcNameToFind = NpcNames.Enemy | NpcNames.Neutral;

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

        public bool Search(string source, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && !playerReader.Bits.PlayerInCombat
                && classConfig.TargetNearestTarget.MillisecondsSinceLastClick > random.Next(1000, 1500))
            {
                if (LookForTarget(source, cancellationToken))
                {
                    if (playerReader.HasTarget && !playerReader.Bits.TargetIsDead)
                    {
                        logger.LogInformation($"{source}: Has target!");
                        return true;
                    }
                    else
                    {
                        if (!cancellationToken.IsCancellationRequested)
                            input.TapClearTarget($"{source}: Target is dead!");

                        if (!cancellationToken.IsCancellationRequested)
                            wait.Update(1);
                    }
                }
            }

            return false;
        }

        private bool LookForTarget(string source, CancellationToken cancellationToken)
        {
            if (playerReader.HasTarget && !playerReader.Bits.TargetIsDead && !blacklist.IsTargetBlacklisted())
            {
                return true;
            }

            if (!cancellationToken.IsCancellationRequested &&
                !input.IsKeyDown(input.TurnLeftKey) && !input.IsKeyDown(input.TurnRightKey))
            {
                npcNameTargeting.ChangeNpcType(NpcNameToFind);
                input.TapNearestTarget(source);
            }

            if (!classConfig.KeyboardOnly && !playerReader.HasTarget && !cancellationToken.IsCancellationRequested)
            {
                npcNameTargeting.ChangeNpcType(NpcNameToFind);
                if (npcNameTargeting.NpcCount > 0 && !cancellationToken.IsCancellationRequested)
                {
                    if (!input.IsKeyDown(input.TurnLeftKey) && !input.IsKeyDown(input.TurnRightKey))
                    {
                        npcNameTargeting.TargetingAndClickNpc(true, cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                            wait.Update(1);
                    }
                }
            }

            return !cancellationToken.IsCancellationRequested && playerReader.HasTarget && !blacklist.IsTargetBlacklisted();
        }
    }
}
