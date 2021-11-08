using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class ItemsBrokenGoal : GoapGoal
    {
        private readonly ILogger logger;
        private readonly PlayerReader playerReader;

        public override float CostOfPerformingAction => 0;

        public ItemsBrokenGoal(PlayerReader playerReader, ILogger logger)
        {
            this.playerReader = playerReader;
            this.logger = logger;
        }

        public override bool CheckIfActionCanRun()
        {
            return playerReader.Bits.ItemsAreBroken;
        }

        public override async ValueTask PerformAction()
        {
            logger.LogInformation("Items are broken");
            SendActionEvent(new ActionEventArgs(GOAP.GoapKey.abort, true));
            await Task.Delay(10000);
        }
    }
}