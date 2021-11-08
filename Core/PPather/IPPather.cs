using Core.PPather;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;

namespace Core
{
    public interface IPPather
    {
        Task<List<Vector3>> FindRoute(int map, Vector3 fromPoint, Vector3 toPoint);
        Task<List<Vector3>> FindRouteTo(AddonReader addonReader, Vector3 wowPoint);
        Task DrawLines(List<LineArgs> lineArgs);
        Task DrawLines();
        Task DrawSphere(SphereArgs args);
    }
}