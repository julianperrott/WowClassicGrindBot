using System.Threading.Tasks;

namespace Libs.Goals
{
    public class NullGoal : GoapGoal
    {
        public override float CostOfPerformingAction => 0;

        public override Task PerformAction()
        {
            return Task.Delay(0);
        }
    }
}