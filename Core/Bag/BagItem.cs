using Core.Addon;
using System;

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
        public int LastChange { get; private set; } = 0;
        public bool IsSoulbound { get; private set; }

        public void UpdateCount(int count)
        {
            if (Count == count)
            {
                return;
            }

            LastUpdated = DateTime.Now;
            LastChange = count - Count;
            LastChangeDescription = LastChange.ToString();
            if (!LastChangeDescription.StartsWith("-")) { LastChangeDescription = $"+{LastChangeDescription}"; }
            this.Count = count;
        }

        private DateTime LastUpdated = DateTime.Now;
        public bool WasRecentlyUpdated => (DateTime.Now - LastUpdated).TotalSeconds < 30;

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