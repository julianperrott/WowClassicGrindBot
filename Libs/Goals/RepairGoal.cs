using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class RepairGoal : NPCGoal
    {
        public RepairGoal(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection, StopMoving stopMoving, ILogger logger, StuckDetector stuckDetector, ClassConfiguration classConfiguration, IPPather pather, BagReader bagReader)
            : base(playerReader, wowProcess, playerDirection, stopMoving, logger, stuckDetector, classConfiguration, pather, bagReader)
        {
            AddPrecondition(GoapKey.incombat, false);
            AddPrecondition(GoapKey.itemsbroken, true);
        }

        public override float CostOfPerformingAction { get => 6f; }

        protected override async Task InteractWithTarget()
        {
            WowPoint location = this.playerReader.PlayerLocation;

            for (int i = 0; i < 5; i++)
            {
                // press interact key
                await this.wowProcess.KeyPress(this.classConfiguration.RepairTarget.ConsoleKey, 200);

                if (!string.IsNullOrEmpty(this.playerReader.Target))
                {
                    await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey, 200);
                }
                else
                {
                    logger.LogError($"Error: No target has been selected. Key {this.classConfiguration.RepairTarget.ConsoleKey} should be /tar an NPC.");
                    break;
                }

                System.Threading.Thread.Sleep(3000);
            }

            if (location.X == this.playerReader.PlayerLocation.X && location.X == this.playerReader.PlayerLocation.Y && this.playerReader.PlayerBitValues.ItemsAreBroken)
            {
                // we didn't move.
                logger.LogError("Error: We didn't move!. Failed to interact with repair. Try again in 10 seconds.");
                failedVendorAttempts++;
                await Task.Delay(10000);
            }
            else if (this.playerReader.PlayerBitValues.ItemsAreBroken)
            {
                // we didn't move.
                logger.LogError("Error: We didn't repair.. Try again in 10 seconds.");
                failedVendorAttempts++;
                await Task.Delay(10000);
            }

            if (failedVendorAttempts == 5)
            {
                logger.LogError("Too many failed repair attempts. Bot stopped.");
                this.SendActionEvent(new ActionEventArgs(GoapKey.abort, true));
            }
        }


        protected override WowPoint GetTargetLocation()
        {
            return this.classConfiguration.RepairLocation;
        }
    }
}
