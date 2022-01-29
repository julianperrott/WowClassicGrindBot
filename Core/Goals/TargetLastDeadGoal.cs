using Core.GOAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class TargetLastDeadGoal : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.2f; }

        private readonly ILogger logger;
        private readonly ConfigurableInput input;
        private bool debug = true;
        

        public TargetLastDeadGoal(ILogger logger, ConfigurableInput input)
        {
            this.logger = logger;
            this.input = input;

            AddPrecondition(GoapKey.hastarget, false);
            AddPrecondition(GoapKey.producedcorpse, true);
        }

        public override ValueTask PerformAction()
        {
            input.TapLastTargetKey(nameof(TargetLastDeadGoal));

            return ValueTask.CompletedTask;
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{nameof(TargetLastDeadGoal)}: {text}");
            }
        }
    }
}