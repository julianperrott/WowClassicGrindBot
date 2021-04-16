using Libs;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace LibsTests
{
    public class RectProvider : IRectProvider
    {
        public void GetWindowRect(out Rectangle rect)
        {
            rect = new Rectangle(0, 0, 1920, 1080);
            //rect = new Rectangle(0, 0, 3840, 2160);
            //rect = new Rectangle(0, 0, 2560, 1440);
        }

        public Task RightClickMouse(Point position)
        {
            throw new NotImplementedException();
        }
    }
}
