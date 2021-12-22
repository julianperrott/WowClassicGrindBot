using System;
using System.Collections.Generic;

namespace Core
{
    public class ActionBarCostEventArgs : EventArgs
    {
        public readonly int index;
        public readonly PowerType powerType;
        public readonly int cost;

        public ActionBarCostEventArgs(int index, PowerType powerType, int cost)
        {
            this.index = index;
            this.powerType = powerType;
            this.cost = cost;
        }
    }

    public class ActionBarCostReader
    {
        private readonly ISquareReader reader;
        private readonly int cActionbarNum;

        private readonly float MAX_POWER_TYPE = 1000000f;
        private readonly float MAX_ACTION_IDX = 1000f;

        //https://wowwiki-archive.fandom.com/wiki/ActionSlot
        private readonly Dictionary<int, (PowerType type, int cost)> dict = new Dictionary<int, (PowerType, int)>();

        private readonly (PowerType type, int cost) empty = (PowerType.Mana, 0);

        public int MaxCount { get; } = 108; // maximum amount of actionbar slot which tracked

        public int Count => dict.Count;

        public event EventHandler<ActionBarCostEventArgs>? OnActionCostChanged;

        public ActionBarCostReader(ISquareReader reader, int cActionbarNum)
        {
            this.cActionbarNum = cActionbarNum;
            this.reader = reader;
        }

        public void Read()
        {
            // formula
            // MAX_POWER_TYPE * type + MAX_ACTION_IDX * slot + cost
            int data = reader.GetIntAtCell(cActionbarNum);
            if (data == 0) return;

            int type = (int)(data / MAX_POWER_TYPE);
            data -= (int)MAX_POWER_TYPE * type;

            int index = (int)(data / MAX_ACTION_IDX);
            data -= (int)MAX_ACTION_IDX * index;

            int cost = data;

            if (dict.TryGetValue(index, out var tuple) && tuple.cost != cost)
            {
                dict.Remove(index);
            }

            if (dict.TryAdd(index, ((PowerType)type, cost)))
            {
                OnActionCostChanged?.Invoke(this, new ActionBarCostEventArgs(index, (PowerType)type, cost));
            }
        }

        public void Reset()
        {
            dict.Clear();
        }

        public (PowerType type, int cost) GetCostByActionBarSlot(PlayerReader playerReader, KeyAction keyAction)
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
