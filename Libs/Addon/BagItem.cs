using Libs.Addon;
using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class BagItem
    {
        public int Bag { get; private set; }
        public int ItemId { get; private set; }
        public int BagIndex { get; private set; }
        public int Count { get; private set; }
        public Item Item { get; private set; }
        public string LastChangeDescription { get; private set; } = "New";


        public void UpdateCount(int count)
        {
            if (Count == count)
            {
                return;
            }

            LastUpdated = DateTime.Now;
            LastChangeDescription = (count - Count).ToString();
            if (!LastChangeDescription.StartsWith("-")) { LastChangeDescription = $"+{LastChangeDescription}"; }
            this.Count = count;
        }

        public DateTime LastUpdated = DateTime.Now;
        public bool WasRecentlyUpdated => (DateTime.Now - LastUpdated).TotalSeconds < 30;

        public BagItem(int bag,int bagIndex, int itemId, int count, Item item)
        {
            this.Bag = bag;
            this.BagIndex = bagIndex;
            this.ItemId = itemId;
            this.Count = count;
            this.Item = item;
        }
    }
}
