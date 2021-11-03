using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Core.GOAP;

namespace Core.Goals
{
    public class ConsumeCorpse : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.1f; }

        public override bool Repeatable { get; } = false;

        private readonly ILogger logger;
        private readonly ClassConfiguration classConfig;

        public ConsumeCorpse(ILogger logger, ClassConfiguration classConfig)
        {
            this.logger = logger;
            this.classConfig = classConfig;

            AddPrecondition(GoapKey.dangercombat, false);

            AddPrecondition(GoapKey.producedcorpse, true);
            AddPrecondition(GoapKey.consumecorpse, false);

            AddEffect(GoapKey.producedcorpse, false);
            AddEffect(GoapKey.consumecorpse, true);

            if (classConfig.Loot)
            {
                AddEffect(GoapKey.shouldloot, true);

                if (classConfig.Skin)
                {
                    AddEffect(GoapKey.shouldskin, true);
                }
            }
        }

        public override async Task PerformAction()
        {
            logger.LogWarning("----- Safe to consume a corpse.");
            SendActionEvent(new ActionEventArgs(GoapKey.consumecorpse, true));

            if (classConfig.Loot)
            {
                SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, true));
            }

            await Task.Delay(5);
        }
    }
}
