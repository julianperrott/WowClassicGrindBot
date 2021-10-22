using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Core.Talents;

namespace Core.Database
{
    public class TalentDB
    {
        private readonly SpellDB spellDB;

        private List<TalentTab> talentTabs = new List<TalentTab>();
        private List<TalentTreeElement> talentTreeElements = new List<TalentTreeElement>();

        public TalentDB(ILogger logger, DataConfig dataConfig, SpellDB spellDB)
        {
            this.spellDB = spellDB;

            talentTabs = JsonConvert.DeserializeObject<List<TalentTab>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "talenttab.json")));
            talentTreeElements = JsonConvert.DeserializeObject<List<TalentTreeElement>>(File.ReadAllText(Path.Join(dataConfig.Dbc, "talent.json")));
        }

        public void Update(Talent talent, PlayerClassEnum playerClassEnum)
        {
            if (talentTabs.Count == 0 || talentTreeElements.Count == 0)
                return;

            string playerClass = playerClassEnum.ToString().ToLower();
            int tabIndex = talent.TabNum - 1;
            int tierIndex = talent.TierNum - 1;
            int columnIndex = talent.ColumnNum - 1;

            var talentTab = talentTabs.First(x => x.BackgroundFile.ToLower().Contains(playerClass) && x.OrderIndex == tabIndex);
            var talentElement = talentTreeElements.First(x => x.TabID == talentTab.Id && x.TierID == tierIndex && x.ColumnIndex == columnIndex);

            var spellId = talentElement.SpellIds[talent.CurrentRank - 1];

            if (spellDB.Spells.TryGetValue(spellId, out Spell spell))
            {
                talent.Name = spell.Name;
            }
        }

    }
}
