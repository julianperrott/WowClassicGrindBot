using System.Drawing;
using System.Threading.Tasks;
using Game;

namespace CoreTests
{
    public class MockWoWProcess : IMouseInput
    {
        public void RightClickMouse(Point position)
        {
            throw new System.NotImplementedException();
        }

        public void LeftClickMouse(Point position)
        {
            throw new System.NotImplementedException();
        }

        public void SetCursorPosition(Point point)
        {
            throw new System.NotImplementedException();
        }
    }
}
