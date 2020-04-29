using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Libs
{
    public class ClassConfiguration
    {
        public string ClassName { get; set; } = string.Empty;
        public bool Loot { get; set; } = true;

        public string PathFilename { get; set; } = string.Empty;
        public string SpiritPathFilename { get; set; } = string.Empty;
        public bool PathThereAndBack = true;
        public bool PathReduceSteps = false;

        public int NPCMaxLevels_Above = 1;
        public int NPCMaxLevels_Below = -7;
        public List<string> Blacklist { get; set; } = new List<string>();

        public KeyConfigurations Pull { get; set; } = new KeyConfigurations();
        public KeyConfigurations Combat { get; set; } = new KeyConfigurations();
        public KeyConfigurations Adhoc { get; set; } = new KeyConfigurations();

        public List<KeyConfiguration> ShapeshiftForm { get; set; } = new List<KeyConfiguration>();

        public KeyConfiguration Interact { get; set; } = new KeyConfiguration();
        public string InteractKey { get; set; } = "H";

        public static Dictionary<ShapeshiftForm, ConsoleKey> ShapeshiftFormKeys = new Dictionary<ShapeshiftForm, ConsoleKey>();

        public void Initialise(RequirementFactory requirementFactory, ILogger logger)
        {
            Pull.Initialise(requirementFactory, logger);
            Combat.Initialise(requirementFactory, logger);
            Adhoc.Initialise(requirementFactory, logger);
            ShapeshiftForm.ForEach(i => i.Initialise(requirementFactory, logger));

            Interact.Key = InteractKey;
            Interact.Initialise(requirementFactory, logger);
        }
    }
}
