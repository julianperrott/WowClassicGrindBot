using System;

namespace Core
{
    public struct CreatureRecord
    {
        public int Guid { get; set; }
        public float HealthPercent { get; set; }
        public DateTime LastEvent { get; set; }

        public bool HasExpired(int seconds)
        {
            return (DateTime.Now - LastEvent).TotalSeconds > seconds;
        }


        public override string ToString()
        {
            return $"guid: {Guid} | hp: {HealthPercent}";
        }

        public override bool Equals(object obj)
        {
            return obj is CreatureRecord other && other.Guid == Guid;
        }

        public override int GetHashCode()
        {
            return Guid;
        }

        public static bool operator ==(CreatureRecord left, CreatureRecord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CreatureRecord left, CreatureRecord right)
        {
            return !(left == right);
        }
    }
}
