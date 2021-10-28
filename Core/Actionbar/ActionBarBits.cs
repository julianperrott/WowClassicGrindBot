namespace Core
{
    public class ActionBarBits
    {
        private readonly BitStatus[] bits;
        private readonly PlayerReader playerReader;

        public ActionBarBits(PlayerReader playerReader, ISquareReader reader, params int[] cells)
        {
            this.playerReader = playerReader;

            bits = new BitStatus[cells.Length];
            for (int i = 0; i < bits.Length; i++)
            {
                bits[i] = new BitStatus(reader.GetIntAtCell(cells[i]));
            }
        }

        // https://wowwiki-archive.fandom.com/wiki/ActionSlot
        public bool Is(KeyAction item)
        {
            if (KeyReader.ActionBarSlotMap.TryGetValue(item.Key, out int slot))
            {
                slot += Stance.RuntimeSlotToActionBar(item, playerReader, slot);

                int array = slot / 24;
                return bits[array].IsBitSet((slot - 1) % 24);
            }

            return false;
        }

        public int Num(KeyAction item)
        {
            if (KeyReader.ActionBarSlotMap.TryGetValue(item.Key, out int slot))
            {
                slot += Stance.RuntimeSlotToActionBar(item, playerReader, slot);
                return slot;
            }

            return 0;
        }
    }
}
