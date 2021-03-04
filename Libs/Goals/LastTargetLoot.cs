using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class LastTargetLoot : GoapGoal
    {
        private ILogger logger;
        private readonly WowInput wowInput;

        private readonly ClassConfiguration classConfiguration;
        private readonly PlayerReader playerReader;
        
        public override float CostOfPerformingAction { get => 4.3f; }

        public LastTargetLoot(ILogger logger, WowInput wowInput, PlayerReader playerReader,  ClassConfiguration classConfiguration)
        {
            this.logger = logger;
            this.wowInput = wowInput;
            this.playerReader = playerReader;
            
            this.classConfiguration = classConfiguration;
        }

        public virtual void AddPreconditions()
        {
            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, false);
        }

        public override async Task PerformAction()
        {
            long lastHealth = playerReader.HealthCurrent;
            WowPoint lastPosition = playerReader.PlayerLocation;

            SendActionEvent(new ActionEventArgs(GoapKey.shouldskin, !playerReader.Unskinnable));

            await wowInput.TapInteractKey("interact target");
            while (IsPlayerMoving(lastPosition))
            {
                logger.LogInformation("wait till the player become stil!");
                lastPosition = playerReader.PlayerLocation;
                if (!await Wait(100, playerReader.HealthCurrent < lastHealth)) { return; }
            }

            if (!await Wait(100, playerReader.HealthCurrent < lastHealth)) { return; }
            await wowInput.TapInteractKey("Looting...");

            // wait grabbing the loot
            if (!await Wait(200, playerReader.HealthCurrent < lastHealth)) { return; }

            logger.LogDebug("Loot was Successfull");
            SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));

            //clear target
            //await wowProcess.KeyPress(ConsoleKey.F3, 50);
            await wowInput.TapClearTarget();
        }

        public override void ResetBeforePlanning()
        {
            base.ResetBeforePlanning();
        }

        private bool IsPlayerMoving(WowPoint lastPos)
        {
            var distance = WowPoint.DistanceTo(lastPos, playerReader.PlayerLocation);
            return distance > 0.5f;
        }
    }
}
