namespace SharedLib.NpcFinder
{
    public struct LineOfNpcName
    {
        public int XStart;
        public int Y;
        public int XEnd;

        public bool IsInAgroup;

        public int Length => XEnd - XStart + 1;
        public int X => XStart + ((XEnd - XStart) / 2);

        public LineOfNpcName(int xStart, int xend, int y)
        {
            this.XStart = xStart;
            this.Y = y;
            this.XEnd = xend;

            this.IsInAgroup = false;
        }
    }
}