using Core;
using SharedLib;
using System.Drawing;

namespace CoreTests
{
    public class RectProvider : IRectProvider
    {
        public RectProvider()
        {
        }

        public void GetRectangle(out Rectangle rect)
        {
            rect = new Rectangle(0, 0, 1920, 1080);
            //rect = new Rectangle(0, 0, 3840, 2160);
            //rect = new Rectangle(0, 0, 2560, 1440);
            //WowScreen.GetRectangle(out rect);
        }

        public void GetPosition(out Point p)
        {
            p = new Point();
        }
    }
}
