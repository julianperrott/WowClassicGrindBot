using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class EquipmentReader
    {
        public readonly int cellStart;
        public readonly ISquareReader reader;

        private long[] equipment = new long[20];

        public EquipmentReader(ISquareReader reader,int cellStart)
        {
            this.cellStart = cellStart;
            this.reader = reader;
        }

        public long[] Read()
        {
            var index = reader.GetLongAtCell(cellStart+1) - 1;
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
    }
}
