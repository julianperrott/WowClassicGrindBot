using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly ILogger logger;
        private readonly WowProcess wowProcess;
        private readonly ClassConfiguration config;
        private readonly AddonReader addonReader;

        public ActionBarPopulator(ILogger logger, WowProcess wowProcess, ClassConfiguration config, AddonReader addonReader)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;
            this.config = config;
            this.addonReader = addonReader;
        }

        private readonly List<ActionBarSource> sources = new List<ActionBarSource>();


        public async Task Execute()
        {
            CollectKeyActions();
            await Run();
        }

        private void CollectKeyActions()
        {
            sources.Clear();

            config.Adhoc.Sequence.ForEach(k => AddUnique(k));
            config.Parallel.Sequence.ForEach(k => AddUnique(k));
            config.Pull.Sequence.ForEach(k => AddUnique(k));
            config.Combat.Sequence.ForEach(k => AddUnique(k));
            config.NPC.Sequence.ForEach(k => AddUnique(k));

            ResolveConsumables();

            sources.Sort((a, b) => a.Key.CompareTo(b.Key));
        }


        private void AddUnique(KeyAction a)
        {
            if (!KeyReader.KeyMapping.ContainsKey(a.Key)) return;
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
            wowProcess.SetForegroundWindow();
            foreach(var a in sources)
            {
                var content = ScriptBuilder(a);
                logger.LogInformation(content);

                wowProcess.SetClipboard(content);
                await Task.Delay(50);
                
                // Open chat inputbox
                await wowProcess.KeyPress(ConsoleKey.Enter, 50);

                // Send Paste keys
                wowProcess.PasteFromClipboard();
                await Task.Delay(250);

                //
                await wowProcess.KeyPress(ConsoleKey.Enter, 50);
                await Task.Delay(250);
            }
        }


        #region Consumable

        private void ResolveConsumables()
        {
            ReplaceIfExists("Water", 
                addonReader.BagReader.HighestQuantityOfWaterId().ToString());

            ReplaceIfExists("Food",
                addonReader.BagReader.HighestQuantityOfFoodId().ToString());
        }

        private void ReplaceIfExists(string key, string val)
        {
            int index = sources.FindIndex(i => i.Name == key);
            if (index != -1)
            {
                var item = sources[index];
                item.Item = true;
                item.Name = val;
                sources[index] = item;
            }
        }

        #endregion


        private static string ScriptBuilder(ActionBarSource a)
        {
            string nameOrId = $"\"{a.Name}\"";
            if (int.TryParse(a.Name, out int id))
            {
                nameOrId = id.ToString();
            }

            string func = GetFunction(a);
            string slot = GetActionBarSlotHotkey(a);
            return $"/run {func}({nameOrId})PlaceAction({slot})ClearCursor()--";
        }

        private static string GetFunction(ActionBarSource a)
        {
            if (a.Item)
                return "PickupItem";

            if (char.IsLower(a.Name[0]))
                return "PickupMacro";

            return "PickupSpellBookItem";
        }
        
        private static string GetActionBarSlotHotkey(ActionBarSource a)
        {
            if(a.Key.StartsWith(KeyReader.BR))
                return CalculateActionNumber(a.Key, KeyReader.BR, KeyReader.BRIdx);

            if (a.Key.StartsWith(KeyReader.BL))
                return CalculateActionNumber(a.Key, KeyReader.BL, KeyReader.BLIdx);

            return CalculateActionNumber(a.Key, "",  0);
        }

        private static string CalculateActionNumber(string key, string prefix,  int offset)
        {
            if(!string.IsNullOrEmpty(prefix))
                key = key.Replace(prefix, "");

            if (int.TryParse(key, out var hotkey))
            {
                if (hotkey == 0)
                    return (offset + 10).ToString();

                return (offset + hotkey).ToString();
            }

            return key;
        }

    }
}
