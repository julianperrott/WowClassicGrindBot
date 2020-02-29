using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class NullAction : GoapAction
    {
        public override float CostOfPerformingAction => throw new NotImplementedException();

        public override Task PerformAction()
        {
            throw new NotImplementedException();
        }
    }
}
