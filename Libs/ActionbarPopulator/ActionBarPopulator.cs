using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libs
{
    public class ActionBarPopulator
    {
        struct ActionBarSource {
            public string Name;
            public string Key;
            public bool Item;
            public string Requirement;
        }

        private ClassConfiguration Config { get; set; }
        private WowProcess WowProcess { get; set; }

        public ActionBarPopulator(ClassConfiguration config, WowProcess process)
        {
            Config = config;
            WowProcess = process;
        }

        private List<ActionBarSource> sources = new List<ActionBarSource>();


        public async Task Execute()
        {
            CollectKeyActions();
            await Run();
        }

        private void CollectKeyActions()
        {
            Config.Adhoc.Sequence.ForEach(k => AddIfNotExists(k));
            Config.Parallel.Sequence.ForEach(k => AddIfNotExists(k));
            Config.Pull.Sequence.ForEach(k => AddIfNotExists(k));
            Config.Combat.Sequence.ForEach(k => AddIfNotExists(k));
            Config.NPC.Sequence.ForEach(k => AddIfNotExists(k));

            TryToResolveConsumables();

            sources.Sort((a, b) => int.Parse(a.Key).CompareTo(int.Parse(b.Key)));
        }


        private void AddIfNotExists(KeyAction a)
        {
            // Only adding those which can be converted to ConsoleKeys
            if (!int.TryParse(a.Key, out int key)) return;
            if (sources.Any(i => i.Key == a.Key)) return;

            var source = new ActionBarSource
            {
                Name = a.Name,
                Key = a.Key,
                Item = false,
                Requirement = a.Requirement
            };

            sources.Add(source);
        }

        private async Task Run()
        {
            foreach(var a in sources)
            {
                NativeMethods.SetClipboardText(ScriptBuilder(a));
                await Task.Delay(50);
                
                await WowProcess.KeyPress(ConsoleKey.Enter, 50);

                WowProcess.SendKeys("^{v}");
                await Task.Delay(250);

                await WowProcess.KeyPress(ConsoleKey.Enter, 50);

                await Task.Delay(250);
            }
        }


        #region Consumable Matching

        private void TryToResolveConsumables()
        {
            TryToMatchConjureSpellToItem("Water");
            TryToMatchConjureSpellToItem("Food");
        }

        private void TryToMatchConjureSpellToItem(string key)
        {
            var index = sources.FindIndex(i => i.Name == key);
            if (index == -1) return;
            var itemsToBeResolved = sources.Single(i => i.Name == key);

            if (!sources.Any(i => i.Name != key && i.Name.Contains(key))) return;
            var match = sources.Single(i => i.Name != key && i.Name.Contains(key));

            itemsToBeResolved.Name = match.Requirement.Split(":")[1];
            itemsToBeResolved.Item = true;
            sources[index] = itemsToBeResolved;
        }

        #endregion


        #region Macro tempaltes

        private static string ScriptBuilder(ActionBarSource a)
        {
            string nameOrId = $"\"{a.Name}\"";
            if (int.TryParse(a.Name, out int id))
            {
                nameOrId = id.ToString();
            }

            string func = GetFunction(a);
            return $"/run {func}({nameOrId})PlaceAction({a.Key})ClearCursor()--";
        }

        private static string GetFunction(ActionBarSource a)
        {
            if (a.Item)
                return "PickupItem";

            if (char.IsLower(a.Name[0]))
                return "PickupMacro";
            else
                return "PickupSpellBookItem";
        }

        #endregion

    }
}
