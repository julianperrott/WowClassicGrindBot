using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Libs.Goals
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
            return playerReader.PlayerBitValues.ItemsAreBroken;
        }

        public override Task PerformAction()
        {
            logger.LogInformation("Items are broken");
            SendActionEvent(new ActionEventArgs(GOAP.GoapKey.abort, true));
            return Task.Delay(10000);
        }
    }
}