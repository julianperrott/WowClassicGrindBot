namespace Core
{
    public class ActionBarBits
    {
        private readonly ActionBarBitStatus[] bits;
        private readonly PlayerReader playerReader;

        public ActionBarBits(PlayerReader playerReader, ISquareReader reader, params int[] cells)
        {
            this.playerReader = playerReader;

            bits = new ActionBarBitStatus[cells.Length];
            for (int i = 0; i < bits.Length; i++)
            {
                bits[i] = new ActionBarBitStatus((int)reader.GetLongAtCell(cells[i]));
            }
        }

        // https://wowwiki-archive.fandom.com/wiki/ActionSlot
        public bool Is(KeyAction item)
        {
            if (KeyReader.ActionBarSlotMap.TryGetValue(item.Key, out int slot))
            {
                slot += Stance.MapActionBar(playerReader, slot);

                int array = (int)(slot / 24);
                return bits[array].IsBitSet((slot - 1) % 24);
            }

            return false;
        }
    }
}
