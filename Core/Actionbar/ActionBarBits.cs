
namespace Core
{
    public class ActionBarBits
    {
        private readonly ActionBarBitStatus[] bits;

        public ActionBarBits(ISquareReader reader, params int[] cells)
        {
            bits = new ActionBarBitStatus[cells.Length];
            for (int i = 0; i < bits.Length; i++)
            {
                bits[i] = new ActionBarBitStatus((int)reader.GetLongAtCell(cells[i]));
            }
        }

        // https://wowwiki-archive.fandom.com/wiki/ActionSlot
        public bool Is(string keyName)
        {
            if (KeyReader.ActionBarSlotMap.TryGetValue(keyName, out int slot))
            {
                int array = (int)(slot / 24);
                return bits[array].IsBitSet((slot - 1) % 24);
            }

            return false;
        }
    }
}
