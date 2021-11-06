using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Goals
{
    public interface ITargetFinder
    {
        Task<bool> Search(string source, CancellationToken cancellationToken);
    }
}
