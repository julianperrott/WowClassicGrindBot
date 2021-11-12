using System;
using System.Collections.Generic;

namespace Core
{
    public class ActionBarCooldownReader
    {
        private readonly ISquareReader reader;
        private readonly int cActionbarNum;

        private readonly float MAX_ACTION_IDX = 100000f;
        private readonly float MAX_VALUE_MUL = 100f;

        private readonly Dictionary<int, Tuple<int, DateTime>> dict = new Dictionary<int, Tuple<int, DateTime>>();

        public ActionBarCooldownReader(ISquareReader reader, int cActionbarNum)
        {
            this.reader = reader;
            this.cActionbarNum = cActionbarNum;
        }

        public void Read()
        {
            // formula
            // MAX_ACTION_IDX * index + (cooldown / MAX_VALUE_MUL)
            float newCooldown = reader.GetIntAtCell(cActionbarNum);
            if (newCooldown == 0) return;

            int index = (int)(newCooldown / MAX_ACTION_IDX);
            newCooldown -= (int)MAX_ACTION_IDX * index;

            newCooldown /= MAX_VALUE_MUL;

            if (dict.TryGetValue(index, out var tuple) && tuple.Item1 != (int)newCooldown)
            {
                dict.Remove(index);
            }

            dict.TryAdd(index, Tuple.Create((int)newCooldown, DateTime.Now));
        }

        public void Reset()
        {
            dict.Clear();
        }

        public int GetRemainingCooldown(PlayerReader playerReader, KeyAction keyAction)
        {
            if (KeyReader.ActionBarSlotMap.TryGetValue(keyAction.Key, out int slot))
            {
                if (slot <= 12)
                {
                    slot += Stance.RuntimeSlotToActionBar(keyAction, playerReader, slot);
                }

                if (dict.TryGetValue(slot, out var tuple))
                {
                    if (tuple.Item1 == 0) return 0;
                    return Math.Clamp((int)(tuple.Item2.AddSeconds(tuple.Item1) - DateTime.Now).TotalMilliseconds, 0, int.MaxValue);
                }
            }

            return 0;
        }

    }
}
