using SharedLib;
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

        private DateTime lastEvent;

        public List<BagItem> BagItems { get; private set; } = new List<BagItem>();

        public Bag[] Bags { get; private set; } = new Bag[5];

        public event EventHandler? DataChanged;

        private bool changedFromEvent;

        public BagReader(ISquareReader reader, ItemDB itemDb, EquipmentReader equipmentReader, int cbagMeta, int citemNumCount, int cItemId, int cItemBits)
        {
            this.reader = reader;
            this.itemDb = itemDb;
            this.equipmentReader = equipmentReader;

            this.equipmentReader.OnEquipmentChanged -= OnEquipmentChanged;
            this.equipmentReader.OnEquipmentChanged += OnEquipmentChanged;

            this.cBagMeta = cbagMeta;
            this.cItemNumCount = citemNumCount;
            this.cItemId = cItemId;
            this.cItemBits = cItemBits;

            for (int i = 0; i < Bags.Length; i++)
            {
                Bags[i] = new Bag();
                if (i == 0)
                {
                    Bags[i].Name = "Backpack";
                }
            }
        }

        public void Read()
        {
            ReadBagMeta(out bool metaChanged);

            ReadInventory(out bool inventoryChanged);

            if (changedFromEvent || metaChanged || inventoryChanged || (DateTime.UtcNow - this.lastEvent).TotalSeconds > 11)
            {
                changedFromEvent = false;
                DataChanged?.Invoke(this, EventArgs.Empty);
                lastEvent = DateTime.UtcNow;
            }
        }

        private void ReadBagMeta(out bool changed)
        {
            changed = false;

            //bagType * 1000000 + bagNum * 100000 + freeSlots * 1000 + self:bagSlots(bagNum)
            int data = reader.GetIntAtCell(cBagMeta);
            if (data == 0) return;

            int bagType = (int)(data / 1000000f);
            data -= 1000000 * bagType;

            int index = (int)(data / 100000f);
            data -= 100000 * index;

            int freeSlots = (int)(data / 1000f);
            data -= 1000 * freeSlots;

            int slotCount = data;

            if (index >= 0 && index < Bags.Length)
            {
                Bag bag = Bags[index];

                // default bag, the first has no equipment slot
                if (index != 0)
                {
                    bag.ItemId = equipmentReader.GetId((int)InventorySlotId.Bag_0 + index - 1);
                    UpdateBagName(index);
                }

                bag.BagType = (BagType)bagType;
                bag.SlotCount = slotCount;
                bag.FreeSlot = freeSlots;

                BagItems.RemoveAll(b => b.Bag == index && b.BagIndex > bag.SlotCount);

                changed = true;
            }
        }

        private void ReadInventory(out bool hasChanged)
        {
            hasChanged = false;

            // 20 -- 0-4 bagNum + 1-21 itenNum + 1-1000 quantity
            int itemCount = reader.GetIntAtCell(cItemNumCount);
            if (itemCount == 0) return;

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
                    if (itemDb.Items.TryGetValue(itemId, out var item))
                    {
                        BagItems.Add(new BagItem(bag, slot, itemId, itemCount, item, isSoulbound));
                        hasChanged = true;
                    }
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

        public int SlotCount => Bags.Sum((x) => x.SlotCount);

        public bool BagsFull => Bags.Sum((x) => x.BagType == BagType.Unspecified ? x.FreeSlot : 0) == 0;

        public bool AnyGreyItem => BagItems.Any((x) => x.Item.Quality == 0);

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

        private void OnEquipmentChanged(object? s, (int, int) tuple)
        {
            if (tuple.Item1 is >= ((int)InventorySlotId.Bag_0) and <= ((int)InventorySlotId.Bag_3))
            {
                int index = tuple.Item1 - (int)InventorySlotId.Tabard;
                Bags[index].ItemId = tuple.Item2;

                UpdateBagName(index);

                changedFromEvent = true;
            }
        }

        private void UpdateBagName(int index)
        {
            if (itemDb.Items.TryGetValue(Bags[index].ItemId, out var item))
            {
                Bags[index].Name = item.Name;
            }
            else
            {
                Bags[index].Name = string.Empty;
            }
        }
    }
}