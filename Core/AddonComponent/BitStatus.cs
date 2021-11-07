using System.Diagnostics;
using System.Text;

namespace Core
{
    public class BitStatus
    {
        private readonly int value;

        public BitStatus(int value)
        {
            this.value = value;
        }

        public bool IsBitSet(int pos)
        {
            return (value & (1 << pos)) != 0;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 1; i < 24; i++)
            {
                sb.Append($"{i}:{IsBitSet(i - 1)},");
            }

            return sb.ToString();
        }

        internal void Dump()
        {
            Debug.WriteLine(ToString());
        }
    }
}