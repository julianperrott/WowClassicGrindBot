using System;
using SharedLib;

namespace Core
{
    public class BagItem
    {
        public int Bag { get; private set; }
        public int ItemId { get; private set; }
        public int BagIndex { get; private set; }
        public int Count { get; private set; }
        public Item Item { get; private set; }
        public string LastChangeDescription { get; private set; } = "New";
        public int LastChange { get; private set; }
        public bool IsSoulbound { get; private set; }

        public void UpdateCount(int count)
        {
            if (Count == count)
            {
                return;
            }

            LastUpdated = DateTime.UtcNow;
            LastChange = count - Count;
            LastChangeDescription = LastChange.ToString();
            if (!LastChangeDescription.StartsWith("-")) { LastChangeDescription = $"+{LastChangeDescription}"; }
            this.Count = count;
        }

        public static readonly int MaxLifeTime = 30;

        public DateTime LastUpdated { get; set; }
        public bool WasRecentlyUpdated => (DateTime.UtcNow - LastUpdated).TotalSeconds < MaxLifeTime;

        public BagItem(int bag, int bagIndex, int itemId, int count, Item item, bool IsSoulbound)
        {
            this.Bag = bag;
            this.BagIndex = bagIndex;
            this.ItemId = itemId;
            this.Count = count;
            this.Item = item;
            this.IsSoulbound = IsSoulbound;
        }
    }
}