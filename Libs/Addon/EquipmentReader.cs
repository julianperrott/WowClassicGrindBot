using System.Linq;

namespace Libs
{
    public enum InventorySlotId
    {
        ammo,
        head,
        neck,
        shoulder,
        shirt,
        chest,
        waist,
        legs,
        feet,
        wrist,
        hands,
        finger_1,
        finger_2,
        trinket_1,
        trinket_2,
        back,
        main_hand,
        off_hand,
        ranged,
        tabard
    }

    public class EquipmentReader
    {
        private readonly int cellStart;
        private readonly ISquareReader reader;

        private long[] equipment = new long[20];

        public EquipmentReader(ISquareReader reader, int cellStart)
        {
            this.cellStart = cellStart;
            this.reader = reader;
        }

        public long[] Read()
        {
            var index = reader.GetLongAtCell(cellStart + 1) - 1;
            if (index < 20 && index >= 0)
            {
                equipment[index] = reader.GetLongAtCell(cellStart);
            }
            return equipment;
        }

        public string ToStringList()
        {
            return string.Join(", ", equipment.Where(i => i > 0));
        }

        public bool HasRanged()
        {
            return equipment[(int)InventorySlotId.ranged] != 0;
        }
    }
}