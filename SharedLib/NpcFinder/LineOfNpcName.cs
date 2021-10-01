namespace SharedLib.NpcFinder
{
    public class LineOfNpcName
    {
        public int XStart { get; set; }
        public int Y { get; set; }
        public int XEnd { get; set; }
        public bool IsInAgroup { get; set; } = false;

        public int Length => XEnd - XStart + 1;
        public int X => XStart + ((XEnd - XStart) / 2);

        public LineOfNpcName(int xStart, int xend, int y)
        {
            this.XStart = xStart;
            this.Y = y;
            this.XEnd = xend;
        }
    }
}