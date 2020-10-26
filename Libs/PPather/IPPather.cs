using System.Collections.Generic;
using System.Threading.Tasks;

namespace Libs
{
    public interface IPPather
    {
        Task<List<WowPoint>> FindRoute(long map, WowPoint fromPoint, WowPoint toPoint);
        Task<List<WowPoint>> FindRouteTo(WowPoint wowPoint);
    }
}