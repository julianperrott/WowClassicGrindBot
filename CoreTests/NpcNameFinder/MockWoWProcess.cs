using Core;
using System.Drawing;
using System.Threading.Tasks;

namespace CoreTests
{
    public class MockWoWProcess : IMouseInput
    {
        public Task RightClickMouse(Point position)
        {
            throw new System.NotImplementedException();
        }

        public Task LeftClickMouse(Point position)
        {
            throw new System.NotImplementedException();
        }

        public void SetCursorPosition(Point point)
        {
            throw new System.NotImplementedException();
        }
    }
}
