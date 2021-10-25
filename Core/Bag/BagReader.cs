using Core.Addon;
using Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class BagReader
    {
        private readonly int cBagMeta;
        private readonly int cItemNumCount;
        private readonly int cItemId;
        private readonly int cItemBits;

        private readonly ISquareReader reader;
        private readonly ItemDB itemDb;
        private readonly EquipmentReader equipmentReader;

        private DateTime lastEvent = DateTime.Now;

        public List<BagItem> BagItems { get; private set; } = new List<BagItem>();

        private readonly Bag[] bags = new Bag[5];

        public event EventHandler? DataChanged;

        public BagReader(ISquareReader reader, ItemDB itemDb, EquipmentReader equipmentReader, int cbagMeta, int citemNumCount, int cItemId, int cItemBits)
        {
            this.reader = reader;
            this.itemDb = itemDb;
            this.equipmentReader = equipmentReader;

            this.cBagMeta = cbagMeta;
            this.cItemNumCount = citemNumCount;
            this.cItemId = cItemId;
            this.cItemBits = cItemBits;
        }

        public void Read()
        {
            ReadBagMeta();

            ReadInventory(out bool hasChanged);

            if (hasChanged || (DateTime.Now - this.lastEvent).TotalSeconds > 11)
            {
                DataChanged?.Invoke(this, new EventArgs());
                lastEvent = DateTime.Now;
            }
        }

        private void ReadBagMeta()
        {
            //bagType * 1000000 + bagNum * 100000 + freeSlots * 1000 + self:bagSlots(bagNum)
            int data = reader.GetIntAtCell(cBagMeta);

            int bagType = (int)(data / 1000000f);
            data -= 1000000 * bagType;

            int index = (int)(data / 100000f);
            data -= 100000 * index;

            int freeSlots = (int)(data / 1000f);
            data -= 1000 * freeSlots;

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
        }

        private void ReadInventory(out bool hasChanged)
        {
            hasChanged = false;

            // 20 -- 0-4 bagNum + 1-21 itenNum + 1-1000 quantity
            int itemCount = reader.GetIntAtCell(cItemNumCount);

            int bag = (int)(itemCount / 1000000f);
            itemCount -= 1000000 * bag;

            int slot = (int)(itemCount / 10000f);
            itemCount -= 10000 * slot;

            // 21 -- 1-999999 itemId
            int itemId = reader.GetIntAtCell(cItemId);

            // 22 -- 0-24 item bits
            int itemBits = reader.GetIntAtCell(cItemBits);

            bool isSoulbound = itemBits == 1;

            var existingItem = BagItems.Where(b => b.BagIndex == slot).Where(b => b.Bag == bag).FirstOrDefault();

            if (itemCount > 0)
            {
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

        public int SlotCount => bags.Sum((x) => x.SlotCount);

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