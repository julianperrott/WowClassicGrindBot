using System;
using System.Collections.Generic;

namespace Core
{
    public enum PowerType
    {
        Mana = 0,
        Rage = 1,
        Focus = 2,
        Energy = 3
    }

    public class ActionBarCostReader
    {
        private readonly ISquareReader reader;
        private readonly int cActionbarNum;

        private readonly float MAX_POWER_TYPE = 1000000f;
        private readonly float MAX_ACTION_IDX = 1000f;

        //https://wowwiki-archive.fandom.com/wiki/ActionSlot
        private readonly Dictionary<int, Tuple<PowerType, int>> dict = new Dictionary<int, Tuple<PowerType, int>>();

        private readonly Tuple<PowerType, int> empty = new Tuple<PowerType, int>(PowerType.Mana,0);

        public int MaxCount { get; } = 108; // maximum amount of actionbar slot which tracked

        public int Count => dict.Count;

        public ActionBarCostReader(ISquareReader reader, int cActionbarNum)
        {
            this.cActionbarNum = cActionbarNum;
            this.reader = reader;
        }

        public void Read()
        {
            // formula
            // MAX_POWER_TYPE * type + MAX_ACTION_IDX * slot + cost
            int data = (int)reader.GetLongAtCell(cActionbarNum);

            int type = (int)(data / MAX_POWER_TYPE);
            data -= (int)MAX_POWER_TYPE * type;

            int index = (int)(data / MAX_ACTION_IDX);
            data -= (int)MAX_ACTION_IDX * index;

            int cost = data;

            if (dict.TryGetValue(index, out var tuple))
            {
                if (tuple.Item2 != cost)
                {
                    dict.Remove(index);
                    dict.Add(index, Tuple.Create((PowerType)type, cost));
                }
            }
            else
            {
                dict.Add(index, Tuple.Create((PowerType)type, cost));
            }
        }

        public void Reset()
        {
            dict.Clear();
        }

        public Tuple<PowerType, int> GetCostByActionBarSlot(PlayerReader playerReader, KeyAction keyAction)
        {
            if (KeyReader.ActionBarSlotMap.TryGetValue(keyAction.Key, out int slot))
            {
                if (slot <= 12)
                {
                    slot += Stance.RuntimeSlotToActionBar(keyAction, playerReader, slot);
                }

                if (dict.TryGetValue(slot, out var tuple))
                {
                    return tuple;
                }
            }

            return empty;
        }
    }
}
