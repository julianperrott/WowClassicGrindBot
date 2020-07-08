using Libs.GOAP;
using Libs.Looting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class TargetDeadAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private bool debug = true;
        private ILogger logger;

        public TargetDeadAction(WowProcess wowProcess, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.logger = logger;

            AddPrecondition(GoapKey.hastarget, true);
            AddPrecondition(GoapKey.targetisalive, false);
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }

        public override float CostOfPerformingAction { get => 4f; }

        public override async Task PerformAction()
        {
            Log("Start PerformAction");

            //this.npcFinder.StopFindingNpcs(10);

            await wowProcess.KeyPress(ConsoleKey.F3, 564);

            Log("End PerformAction");
        }
    }
}