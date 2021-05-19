using Core.Addon;
using Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class BagReader
    {
        private int bagItemsDataStart = 20;
        private int bagInfoDataStart = 60;
        private int bagSlotCountStart = 37;

        private readonly ISquareReader reader;
        private readonly ItemDB itemDb;
        private readonly EquipmentReader equipmentReader;

        private DateTime lastEvent = DateTime.Now;

        public List<BagItem> BagItems { get; private set; } = new List<BagItem>();

        private readonly Bag[] bags = new Bag[5];

        public event EventHandler? DataChanged;

        public BagReader(ISquareReader reader, int bagItemsDataStart, ItemDB itemDb, EquipmentReader equipmentReader)
        {
            this.bagItemsDataStart = bagItemsDataStart;
            this.reader = reader;
            this.itemDb = itemDb;
            this.equipmentReader = equipmentReader;
        }

        public void Read()
        {
            bool hasChanged = false;

            //bagType * 1000000 + bagNum * 100000 + freeSlots * 1000 + self:bagSlots(bagNum)
            int data = (int)reader.GetLongAtCell(bagSlotCountStart);

            int bagType = (int)(data / 1000000f);
            data -= (1000000 * bagType);

            int index = (int)(data / 100000f);
            data -= (100000 * index);

            int freeSlots = (int)(data / 1000f);
            data -= (1000 * freeSlots);

            int slotCount = data;

            if (index >= 0 && index < bags.Length)
            {
                // default bag, the first has no equipment slot
                if (index != 0)
                    bags[index].ItemId = equipmentReader.GetId((int)InventorySlotId.Bag_0 + index - 1);

                bags[index].BagType = (BagType)bagType;
                bags[index].SlotCount = slotCount;
                bags[index].FreeSlot = freeSlots;
            }

            for (var bag = 0; bag < 5; bag++)
            {
                var cellIndex = bagItemsDataStart + (bag * 2);
                var itemCount = reader.Get5Numbers(cellIndex, SquareReader.Part.Left);

                var bagInfoIndex = bagInfoDataStart + bag;
                var isSoulbound = reader.GetLongAtCell(bagInfoIndex) == 1;

                // get bag and slot
                var val = reader.GetLongAtCell(cellIndex + 1);
                var bagNumber = val / 20;
                var slot = (int)(val - bagNumber * 20);

                var existingItem = BagItems.Where(b => b.BagIndex == slot).Where(b => b.Bag == bag).FirstOrDefault();

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
                        if (itemDb.Items.ContainsKey(itemId))
                        {
                            item = itemDb.Items[itemId];
                        }
                        BagItems.Add(new BagItem(bag, slot, itemId, itemCount, item, isSoulbound));
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

        public long SlotCount => bags.Sum((x) => x.SlotCount);

        public bool BagsFull => bags.Sum((x) => x.BagType == BagType.Unspecified ? x.FreeSlot : 0) == 0;

        public int ItemCount(int itemId) => BagItems.Where(bi => bi.ItemId == itemId).Sum(bi => bi.Count);

        public bool HasItem(int itemId) => ItemCount(itemId) != 0;

        public int HighestQuantityOfWaterId()
        {
            return itemDb.WaterIds.
                OrderByDescending(c => ItemCount(c)).
                FirstOrDefault();
        }

        public int HighestQuantityOfFoodId()
        {
            return itemDb.FoodIds.
                OrderByDescending(c => ItemCount(c)).
                FirstOrDefault();
        }
    }
}