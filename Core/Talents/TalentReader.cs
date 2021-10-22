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
            int data = (int)reader.GetLongAtCell(cTalent);
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

            talentDB.Update(talent, playerReader.PlayerClass);

            Talents.Add(hash, talent);
        }

        public void Reset()
        {
            Talents.Clear();
        }
    }
}
