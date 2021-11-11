using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Core.Talents;
using SharedLib;

namespace Core.Database
{
    public class TalentDB
    {
        private readonly SpellDB spellDB;

        private readonly List<TalentTab> talentTabs;
        private readonly List<TalentTreeElement> talentTreeElements;

        public TalentDB(ILogger logger, DataConfig dataConfig, SpellDB spellDB)
        {
            this.spellDB = spellDB;

            talentTabs = JsonConvert.DeserializeObject<List<TalentTab>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "talenttab.json")));
            talentTreeElements = JsonConvert.DeserializeObject<List<TalentTreeElement>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "talent.json")));
        }

        public bool Update(ref Talent talent, PlayerClassEnum playerClassEnum)
        {
            if (talentTabs.Count == 0 || talentTreeElements.Count == 0)
                return false;

            string playerClass = playerClassEnum.ToString().ToLower();
            int tabIndex = talent.TabNum - 1;
            int tierIndex = talent.TierNum - 1;
            int columnIndex = talent.ColumnNum - 1;

            int talentTabIndex = talentTabs.FindIndex(x => x.BackgroundFile.ToLower().Contains(playerClass) && x.OrderIndex == tabIndex);
            if (talentTabIndex == -1) return false;
            int talentElementIndex = talentTreeElements.FindIndex(x => x.TabID == talentTabs[talentTabIndex].Id && x.TierID == tierIndex && x.ColumnIndex == columnIndex);
            if (talentElementIndex == -1) return false;

            var spellId = talentTreeElements[talentElementIndex].SpellIds[talent.CurrentRank - 1];
            if (spellDB.Spells.TryGetValue(spellId, out Spell spell))
            {
                talent.Name = spell.Name;
                return true;
            }

            return false;
        }

    }
}
