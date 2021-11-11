using System.Collections.Generic;
using Core.Talents;
using Core.Database;

namespace Core
{
    public class TalentReader
    {
        private readonly int cTalent;

        private readonly ISquareReader reader;
        private readonly PlayerReader playerReader;
        private readonly TalentDB talentDB;

        public Dictionary<int, Talent> Talents { get; private set; } = new Dictionary<int, Talent>();

        public TalentReader(ISquareReader reader, int cTalent, PlayerReader playerReader, TalentDB talentDB)
        {
            this.reader = reader;
            this.cTalent = cTalent;

            this.playerReader = playerReader;
            this.talentDB = talentDB;
        }

        public void Read()
        {
            int data = reader.GetIntAtCell(cTalent);
            if (data == 0 || Talents.ContainsKey(data)) return;

            int hash = data;

            int tab = (int)(data / 1000000f);
            data -= 1000000 * tab;

            int tier = (int)(data / 10000f);
            data -= 10000 * tier;

            int column = (int)(data / 10f);
            data -= 10 * column;

            var talent = new Talent
            {
                Hash = hash,
                TabNum = tab,
                TierNum = tier,
                ColumnNum = column,
                CurrentRank = data
            };

            if (talentDB.Update(ref talent, playerReader.Class))
            {
                Talents.Add(hash, talent);
            }
        }

        public void Reset()
        {
            Talents.Clear();
        }

        public bool HasTalent(string name, int rank)
        {
            foreach (var kvp in Talents)
            {
                if (kvp.Value.Name.ToLower() == name.ToLower() && kvp.Value.CurrentRank >= rank)
                    return true;
            }

            return false;
        }
    }
}
