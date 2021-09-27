
namespace Core
{
    public class ActionBarCurrentAction
    {
        private readonly ActionBarBitStatus bits_1To24;
        private readonly ActionBarBitStatus bits_25To48;
        private readonly ActionBarBitStatus bits_49To72;
        private readonly ActionBarBitStatus bits_73To96;

        public ActionBarCurrentAction(ISquareReader reader, int idx1, int idx2, int idx3, int idx4)
        {
            bits_1To24 = new ActionBarBitStatus(reader.GetLongAtCell(idx1));
            bits_25To48 = new ActionBarBitStatus(reader.GetLongAtCell(idx2));
            bits_49To72 = new ActionBarBitStatus(reader.GetLongAtCell(idx3));
            bits_73To96 = new ActionBarBitStatus(reader.GetLongAtCell(idx4));
        }

        // https://wowwiki-archive.fandom.com/wiki/ActionSlot
        // valid range 1-96
        public bool Is(string keyName)
        {
            if (KeyReader.ActionBarSlotMap.TryGetValue(keyName, out var slot))
            {
                if (slot < 24)
                    return bits_1To24.IsBitSet(slot - 1);
                if (slot < 48)
                    return bits_25To48.IsBitSet(slot - 1);
                if (slot < 72)
                    return bits_49To72.IsBitSet(slot - 1);
                if (slot < 96)
                    return bits_73To96.IsBitSet(slot - 1);
            }

            return false;
        }

    }
}
