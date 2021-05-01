using System.Threading.Tasks;

namespace Core.Goals
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