using System.Threading.Tasks;

namespace Core.Goals
{
    public class NullGoal : GoapGoal
    {
        public override float CostOfPerformingAction => 0;

        public override async ValueTask PerformAction()
        {
            await Task.Delay(0);
        }
    }
}