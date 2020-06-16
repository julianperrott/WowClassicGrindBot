using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class ItemsBrokenAction : GoapAction
    {
        private readonly ILogger logger;
        private readonly PlayerReader playerReader;

        public override float CostOfPerformingAction => 0;

        public ItemsBrokenAction(PlayerReader playerReader, ILogger logger)
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
            return Task.Delay(1000);
        }
    }
}