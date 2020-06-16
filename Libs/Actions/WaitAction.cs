using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class WaitAction : GoapAction
    {
        private readonly ILogger logger;

        public override float CostOfPerformingAction => 21;

        public WaitAction(ILogger logger)
        {
            this.logger = logger;
        }

        public override Task PerformAction()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.isalive, true));
            logger.LogInformation("Waiting");
            return Task.Delay(1000);
        }
    }
}