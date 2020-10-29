using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class VendorGoal : NPCGoal
    {
        public VendorGoal(PlayerReader playerReader, WowProcess wowProcess, IPlayerDirection playerDirection, StopMoving stopMoving, ILogger logger, StuckDetector stuckDetector, ClassConfiguration classConfiguration, IPPather pather, BagReader bagReader)
            : base(playerReader, wowProcess, playerDirection, stopMoving, logger, stuckDetector, classConfiguration, pather, bagReader)
        {
            AddPrecondition(GoapKey.incombat, false);

            var action = new KeyAction();
            this.Keys.Add(action);

            action.RequirementObjects.Add(
                   new Requirement
                   {
                       HasRequirement = () => this.bagReader.BagItems.Count >= this.classConfiguration.VendorItemThreshold,
                       LogMessage = () => $"Bag items {this.bagReader.BagItems.Count} >= {this.classConfiguration.VendorItemThreshold}"
                   }
                );
        }

        public override bool CheckIfActionCanRun()
        {
            return this.Keys[0].CanRun();
        }

        public override float CostOfPerformingAction { get => 6f; }

        protected override async Task InteractWithTarget()
        {
            WowPoint location = this.playerReader.PlayerLocation;
            var bagItems = this.bagReader.BagItems.Count;

            for (int i = 0; i < 5; i++)
            {
                // press interact key
                await this.wowProcess.KeyPress(this.classConfiguration.VendorTarget.ConsoleKey, 200);

                if (!string.IsNullOrEmpty(this.playerReader.Target))
                {
                    await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey, 200);
                }
                else
                {
                    logger.LogError($"Error: No target has been selected. Key {this.classConfiguration.VendorTarget.ConsoleKey} should be /tar an NPC.");
                    break;
                }

                System.Threading.Thread.Sleep(3000);
            }

            if (location.X == this.playerReader.PlayerLocation.X && location.X == this.playerReader.PlayerLocation.Y && bagItems <= this.bagReader.BagItems.Count)
            {
                // we didn't move.
                logger.LogError("Error: We didn't move!. Failed to interact with vendor. Try again in 10 seconds.");
                failedVendorAttempts++;
                await Task.Delay(10000);
            }
            else if (bagItems <= this.bagReader.BagItems.Count)
            {
                // we didn't move.
                logger.LogError("Error: We didn't sell anything.. Try again in 10 seconds.");
                failedVendorAttempts++;
                await Task.Delay(10000);
            }

            if (failedVendorAttempts == 5)
            {
                logger.LogError("Too many failed vendor attempts. Bot stopped.");
                this.SendActionEvent(new ActionEventArgs(GoapKey.abort, true));
            }
        }

       
        protected override WowPoint GetTargetLocation()
        {
            return this.classConfiguration.VendorLocation;
        }
    }
}