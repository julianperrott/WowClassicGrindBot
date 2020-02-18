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
        
        public BagItem(int bag,int bagIndex, int itemId, int count)
        {
            this.Bag = bag;
            this.BagIndex = bagIndex;
            this.ItemId = itemId;
            this.Count = count;
        }
    }
}
