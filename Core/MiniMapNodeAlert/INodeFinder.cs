using System.Drawing;

namespace Core
{
    public interface INodeFinder
    {
        Point? Find(bool highlight);
    }
}