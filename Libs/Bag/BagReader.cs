using Libs.Addon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Libs
{
    public class BagReader
    {
        private int bagItemsDataStart = 20;
        private readonly ISquareReader reader;

        private DateTime lastEvent = DateTime.Now;

        public List<BagItem> BagItems = new List<BagItem>();

        public Dictionary<int, Item> itemDictionary = new Dictionary<int, Item>();

        public event EventHandler? DataChanged;

        public BagReader(ISquareReader reader, int bagItemsDataStart, List<Item> items)
        {
            this.bagItemsDataStart = bagItemsDataStart;
            this.reader = reader;
            items.ForEach(i => itemDictionary.Add(i.Entry, i));
        }

        public List<BagItem> Read()
        {
            bool hasChanged = false;

            for (var bag = 0; bag < 5; bag++)
            {
                var cellIndex = bagItemsDataStart + (bag * 2);

                var itemCount = reader.Get5Numbers(cellIndex, SquareReader.Part.Left);

                var val = reader.GetLongAtCell(cellIndex + 1);
                var bagNumber = val / 16;
                var bagIndex = (int)(val - bagNumber * 16);

                var existingItem = BagItems.Where(b => b.BagIndex == bagIndex).Where(b => b.Bag == bag).FirstOrDefault();

                if (itemCount > 0)
                {
                    var itemId = reader.Get5Numbers(cellIndex, SquareReader.Part.Right);

                    bool addItem = true;

                    if (existingItem != null)
                    {
                        if (existingItem.ItemId != itemId)
                        {
                            BagItems.Remove(existingItem);
                            addItem = true;
                        }
                        else
                        {
                            addItem = false;

                            if (existingItem.Count != itemCount)
                            {
                                existingItem.UpdateCount(itemCount);
                                hasChanged = true;
                            }
                        }
                    }

                    if (addItem)
                    {
                        var item = new Item { Name = "Unknown" };
                        if (this.itemDictionary.ContainsKey(itemId))
                        {
                            item = itemDictionary[itemId];
                        }
                        BagItems.Add(new BagItem(bag, bagIndex, itemId, itemCount, item));
                        hasChanged = true;
                    }
                }
                else
                {
                    if (existingItem != null)
                    {
                        BagItems.Remove(existingItem);
                        hasChanged = true;
                    }
                }
            }

            if (hasChanged || (DateTime.Now - this.lastEvent).TotalSeconds > 11)
            {
                DataChanged?.Invoke(this, new EventArgs());
                lastEvent = DateTime.Now;
            }

            return BagItems;
        }

        public List<string> ToBagString()
        {
            return Enumerable.Range(0, 5).Select(i =>
                $"Bag {i}: " + string.Join(", ",
                 BagItems.Where(b => b.Bag == i)
                     .OrderBy(b => b.BagIndex)
                     .Select(b => $"{b.ItemId}({b.Count})")
                 ))
                .ToList();
        }

        public bool BagsFull => BagItems.Count >= 76;

        public int ItemCount(int itemId) => BagItems.Where(bi => bi.ItemId == itemId).Sum(bi => bi.Count);
    }
}