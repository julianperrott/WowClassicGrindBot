using System.Threading.Tasks;

namespace Libs.Actions
{
    public class NullAction : GoapAction
    {
        public override float CostOfPerformingAction => 0;

        public override Task PerformAction()
        {
            return Task.Delay(0);
        }
    }
}