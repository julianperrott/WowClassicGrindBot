using Core.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class WaitGoal : GoapGoal
    {
        private readonly ILogger logger;

        public override float CostOfPerformingAction => 21;

        public WaitGoal(ILogger logger)
        {
            this.logger = logger;
        }

        public override async ValueTask PerformAction()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.isalive, true));
            logger.LogInformation("Waiting");
            await Task.Delay(1000);
        }
    }
}