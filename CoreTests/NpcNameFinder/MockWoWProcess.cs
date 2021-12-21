using System.Drawing;
using System.Threading.Tasks;
using Game;

namespace CoreTests
{
    public class MockWoWProcess : IMouseInput
    {
        public ValueTask RightClickMouse(Point position)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask LeftClickMouse(Point position)
        {
            throw new System.NotImplementedException();
        }

        public void SetCursorPosition(Point point)
        {
            throw new System.NotImplementedException();
        }
    }
}
