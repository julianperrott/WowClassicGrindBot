using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class BagReader
    {
        private int bagItemsDataStart = 20;
        public readonly List<DataFrame> frames;
        private readonly ISquareReader reader;

        public List<BagItem> bagItems=new List<BagItem>();

        public BagReader(ISquareReader reader, int bagItemsDataStart, List<DataFrame> frames)
        {
            this.bagItemsDataStart = bagItemsDataStart;
            this.frames = frames;
            this.reader = reader;
        }

        public List<BagItem> Read()
        {
            for (var bag = 0; bag < 5; bag++)
            {
                var cell = bagItemsDataStart + (bag * 2);

                var itemCount = reader.Get5Numbers(frames[cell], SquareReader.Part.Left);

                var val = reader.GetLongAtCell(frames[cell + 1]);
                var bagNumber = val / 16;
                var bagIndex = (int)(val - bagNumber * 16);

                var item = bagItems.Where(b => b.BagIndex == bagIndex).Where(b => b.Bag == bag).FirstOrDefault();

                if (itemCount > 0)
                {
                    var itemId = reader.Get5Numbers(frames[cell], SquareReader.Part.Right);

                    bool addItem = true;

                    if (item != null)
                    {
                        if (item.ItemId!= itemId || item.Count!= itemCount)
                        {
                            bagItems.Remove(item);
                        }
                        else
                        {
                            addItem = false;
                        }
                    }

                    if (addItem)
                    {
                        bagItems.Add(new BagItem(bag, bagIndex, itemId, itemCount));
                    }
                }
                else
                {
                    if (item != null)
                    {
                        bagItems.Remove(item);
                    }
                }
            }
            return bagItems;
        }

        public List<string> ToBagString()
        {
            return Enumerable.Range(0, 5).Select(i =>
                $"Bag {i}: " + string.Join(", ",
                 bagItems.Where(b => b.Bag == i)
                     .OrderBy(b => b.BagIndex)
                     .Select(b => $"{b.ItemId}({b.Count})")
                 ))
                .ToList();
        }
    }
}
