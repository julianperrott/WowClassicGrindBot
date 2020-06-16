using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Libs
{
    public class NoBlacklist: IBlacklist
    {
        public bool IsTargetBlacklisted()
        {
            return false;
        }
    }
}