using System;

namespace Core
{
    public class RecordInt
    {
        private readonly int cell;
        private int temp;

        public int Value { private set; get; }
        public DateTime LastChanged { private set; get; }

        public int ElapsedMs => (int)(DateTime.UtcNow - LastChanged).TotalMilliseconds;

        public event EventHandler? Changed;

        public RecordInt(int cell)
        {
            this.cell = cell;
        }

        public bool Updated(ISquareReader reader)
        {
            temp = reader.GetIntAtCell(cell);
            if (temp != Value)
            {
                Value = temp;
                Changed?.Invoke(this, EventArgs.Empty);
                LastChanged = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        public void Update(ISquareReader reader)
        {
            temp = reader.GetIntAtCell(cell);
            if (temp != Value)
            {
                Value = temp;
                Changed?.Invoke(this, EventArgs.Empty);
                LastChanged = DateTime.UtcNow;
            }
        }

        public void Reset()
        {
            Value = 0;
            temp = 0;
            LastChanged = default;
        }

        public void ForceUpdate(int value)
        {
            Value = value;
        }
    }
}