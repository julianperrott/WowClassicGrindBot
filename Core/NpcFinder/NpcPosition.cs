using System;
using System.Collections.Generic;
using System.Drawing;

namespace Core
{
    public class NpcPosition
    {
        public Point Min { get; set; }
        public Point Max { get; set; }

        public int Height => Max.Y - Min.Y;
        public int Width => Max.X - Min.X;
        private int screenMid;
        private int screenMidBuffer;

        private float heightMul1 = 30;
        private float heightMul2 = 7;

        public bool IsAdd => ClickPoint.X < screenMid - screenMidBuffer || ClickPoint.X > screenMid + screenMidBuffer;

        public Point ClickPoint => new Point(Min.X + (Width / 2), (int)(Max.Y + heightMul1 + (Height * heightMul2)));

        public NpcPosition(Point min, Point max, int screenWidth, float heightMul1, float heightMul2)
        {
            this.Min = min;
            this.Max = max;
            this.screenMid = screenWidth / 2;
            this.screenMidBuffer = screenWidth / 10;

            this.heightMul1 = heightMul1;
            this.heightMul2 = heightMul2;
        }
    }

    public class OverlappingNames : IEqualityComparer<NpcPosition>
    {
        public bool Equals(NpcPosition a, NpcPosition b)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(a, b)) return true;

            //Check whether any of the compared objects is null.
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;

            var ar = new RectangleF(a.Min, new Size(a.Width, a.Height));
            var br = new RectangleF(b.Min, new Size(b.Width, b.Height));
            return ar.IntersectsWith(br);
        }

        public int GetHashCode(NpcPosition obj)
        {
            //Check whether the object is null
            if (ReferenceEquals(obj, null)) return 0;

            //Calculate the hash code for the product.
            return obj.Min.GetHashCode() ^ obj.Max.GetHashCode();

        }
    }
}