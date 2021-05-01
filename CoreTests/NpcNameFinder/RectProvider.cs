using Core;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace CoreTests
{
    public class RectProvider : IRectProvider
    {
        public void GetWindowRect(out Rectangle rect)
        {
            //rect = new Rectangle(0, 0, 1920, 1080);
            //rect = new Rectangle(0, 0, 3840, 2160);
            //rect = new Rectangle(0, 0, 2560, 1440);

            WowProcess process = new WowProcess(null);
            WowScreen screen = new WowScreen(process, null);
            screen.GetRectangle(out rect);
        }

        public Task RightClickMouse(Point position)
        {
            throw new NotImplementedException();
        }
    }
}
