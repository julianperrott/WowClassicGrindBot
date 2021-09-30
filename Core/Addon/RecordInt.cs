using System;

namespace Core
{
    public class RecordInt
    {
        private readonly int cell;
        private int temp;

        public int Value { private set; get; }
        public DateTime LastChanged { private set; get; }

        public long ElapsedMs => (long)(DateTime.Now - LastChanged).TotalMilliseconds;
        
        public RecordInt(int cell)
        {
            this.cell = cell;
        }

        public bool Updated(ISquareReader reader)
        {
            temp = (int)reader.GetLongAtCell(cell);
            if (temp != Value)
            {
                Value = temp;
                LastChanged = DateTime.Now;
                return true;
            }

            return false;
        }
    }
}