using Core.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class TargetLastDeadGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.2f; }

        private readonly ILogger logger;
        private readonly WowInput wowInput;
        private bool debug = true;
        

        public TargetLastDeadGoal(ILogger logger, WowInput wowInput)
        {
            this.logger = logger;
            this.wowInput = wowInput;

            AddPrecondition(GoapKey.hastarget, false);
            AddPrecondition(GoapKey.producedcorpse, true);
        }

        public override async Task PerformAction()
        {
            await wowInput.TapLastTargetKey(this.ToString());
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }
    }
}