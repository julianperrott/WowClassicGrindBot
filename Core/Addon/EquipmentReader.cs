using System;

namespace Core
{
    public enum InventorySlotId
    {
        Ammo = 0,
        Head = 1,
        Neck = 2,
        Shoulder = 3,
        Shirt = 4,
        Chest = 5,
        Waist = 6,
        Legs = 7,
        Feet = 8,
        Wrist = 9,
        Hands = 10,
        Finger_1 = 11,
        Finger_2 = 12,
        Trinket_1 = 13,
        Trinket_2 = 14,
        Back = 15,
        Main_hand = 16,
        Off_hand = 17,
        Ranged = 18,
        Tabard = 19,
        Bag_0 = 20,
        Bag_1 = 21,
        Bag_2 = 22,
        Bag_3 = 23
    }

    public class EquipmentReader
    {
        private const int MAX_EQUIPMENT_COUNT = 24;

        private readonly ISquareReader reader;
        private readonly int cItemId;
        private readonly int cSlotNum;

        private readonly int[] equipment = new int[MAX_EQUIPMENT_COUNT];

        public event EventHandler<(int, int)>? OnEquipmentChanged;

        public EquipmentReader(ISquareReader reader, int cSlotNum, int cItemId)
        {
            this.reader = reader;

            this.cSlotNum = cSlotNum;
            this.cItemId = cItemId;
        }

        public void Read()
        {
            int index = reader.GetIntAtCell(cSlotNum);
            if (index < MAX_EQUIPMENT_COUNT && index >= 0)
            {
                int itemId = reader.GetIntAtCell(cItemId);
                bool changed = equipment[index] != itemId;

                equipment[index] = itemId;

                if (changed)
                    OnEquipmentChanged?.Invoke(this, (index, itemId));
            }
        }

        public string ToStringList()
        {
            return string.Join(", ", equipment);
        }

        public bool HasRanged()
        {
            return equipment[(int)InventorySlotId.Ranged] != 0;
        }

        public bool HasItem(int itemId)
        {
            for (int i = 0; i < equipment.Length; i++)
            {
                if (equipment[i] == itemId)
                    return true;
            }

            return false;
        }

        public int GetId(int slot)
        {
            return (int)equipment[slot];
        }
    }
}