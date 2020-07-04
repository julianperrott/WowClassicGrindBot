using System.Drawing;

namespace Libs
{
    public interface INodeFinder
    {
        Point? Find(bool highlight);
    }
}