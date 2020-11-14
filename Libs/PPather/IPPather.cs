using Libs.PPather;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Libs
{
    public interface IPPather
    {
        Task<List<WowPoint>> FindRoute(int map, WowPoint fromPoint, WowPoint toPoint);
        Task<List<WowPoint>> FindRouteTo(PlayerReader playerReader,WowPoint wowPoint);
        Task DrawLines(List<LineArgs> lineArgs);
        Task DrawLines();
        Task DrawSphere(SphereArgs args);
    }
}