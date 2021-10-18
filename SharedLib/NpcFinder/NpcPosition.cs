using System;
using System.Collections.Generic;
using System.Drawing;

namespace SharedLib.NpcFinder
{
    public class NpcPosition
    {
        public Point Min { get; set; }
        public Point Max { get; set; }

        public int Height => Max.Y - Min.Y;
        public int Width => Max.X - Min.X;

        public readonly int screenMid;
        public readonly int screenTargetBuffer;
        public readonly int screenMidBuffer;
        public readonly int screenAddBuffer;

        private readonly float yOffset;
        private readonly float heightMul;

        public bool IsAdd =>
            (ClickPoint.X < screenMid - screenTargetBuffer && ClickPoint.X > screenMid - screenAddBuffer) ||
            (ClickPoint.X > screenMid + screenTargetBuffer && ClickPoint.X < screenMid + screenAddBuffer);

        public Point ClickPoint => new Point(Min.X + (Width / 2), (int)(Max.Y + yOffset + (Height * heightMul)));

        public NpcPosition(Point min, Point max, int screenWidth, float yOffset, float heightMul)
        {
            Min = min;
            Max = max;
            screenMid = screenWidth / 2;
            screenMidBuffer = screenWidth / 15;
            screenTargetBuffer = screenMidBuffer / 4;
            screenAddBuffer = screenMidBuffer * 3;

            this.yOffset = yOffset;
            this.heightMul = heightMul;
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