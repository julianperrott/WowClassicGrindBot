using System.Drawing;
using Newtonsoft.Json;

namespace Core
{
    public struct DataFrameMeta : System.IEquatable<object>, System.IEquatable<DataFrameMeta>
    {
        [JsonConstructor]
        public DataFrameMeta(int hash, int spacing, int size, int rows, int frames)
        {
            this.hash = hash;
            this.spacing = spacing;
            this.size = size;
            this.rows = rows;
            this.frames = frames;
        }

        public int hash { private set; get; }

        public int spacing { private set; get; }

        public int size { private set; get; }

        public int rows { private set; get; }

        public int frames { private set; get; }

        public Size EstimatedSize()
        {
            var size = new SizeF(frames * (this.size + spacing), rows * (this.size + spacing));
            size *= 1.3f; // error rate
            return size.ToSize();
        }

        private static readonly DataFrameMeta empty = new DataFrameMeta(-1, 0, 0, 0, 0);
        [JsonIgnore]
        public static DataFrameMeta Empty => empty;

        public override bool Equals(object obj)
        {
            if (!(obj is DataFrameMeta other)) return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public static bool operator ==(DataFrameMeta left, DataFrameMeta right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DataFrameMeta left, DataFrameMeta right)
        {
            return !(left == right);
        }

        public bool Equals(DataFrameMeta other)
        {
            return other.hash == hash &&
                other.spacing == spacing &&
                other.size == size &&
                other.rows == rows &&
                other.frames == frames;
        }
    }
}
