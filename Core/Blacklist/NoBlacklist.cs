using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class NoBlacklist: IBlacklist
    {
        public void Add(string name)
        {
        }

        public bool IsTargetBlacklisted()
        {
            return false;
        }
    }
}