using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Libs
{
    public class BadZone
    {
        public int ZoneId { get; set; } = -1;
        public WowPoint ExitZoneLocation { get; set; } = new WowPoint(0, 0);
    }

    public enum Mode
    {
        Grind = 0,
        CorpseRun = 1,
        AttendedGather = 2,
        AttendedGrind = 3
    }


    public class ClassConfiguration
    {
        public string ClassName { get; set; } = string.Empty;
        public bool Loot { get; set; } = true;

        public string PathFilename { get; set; } = string.Empty;
        public string SpiritPathFilename { get; set; } = string.Empty;
        public bool PathThereAndBack { get; set; } = true;
        public bool PathReduceSteps { get; set; } = false;

        public Mode Mode { get; set; } = Mode.Grind;

        public BadZone WrongZone { get; set; } = new BadZone();

        public int NPCMaxLevels_Above { get; set; } = 1;
        public int NPCMaxLevels_Below { get; set; } = 7;
        public List<string> Blacklist { get; } = new List<string>();

        public KeyActions Pull { get; set; } = new KeyActions();
        public KeyActions Combat { get; set; } = new KeyActions();
        public KeyActions Adhoc { get; set; } = new KeyActions();
        public KeyActions Parallel { get; set; } = new KeyActions();

        public List<KeyAction> ShapeshiftForm { get; } = new List<KeyAction>();

        public KeyAction Interact { get; set; } = new KeyAction();
        public KeyAction Blink { get; set; } = new KeyAction();
        public string InteractKey { get; set; } = "H";

        public List<KeyAction> GatherFindKeyConfig { get; } = new List<KeyAction>();
        public List<string> GatherFindKeys { get; } = new List<string>();

        public KeyAction TargetLastTarget { get; set; } = new KeyAction();
        public string TargetLastTargetKey { get; set; } = "N";

        public static Dictionary<ShapeshiftForm, ConsoleKey> ShapeshiftFormKeys { get; private set; } = new Dictionary<ShapeshiftForm, ConsoleKey>();

        public void Initialise(PlayerReader playerReader, RequirementFactory requirementFactory, ILogger logger)
        {
            Pull.Initialise(playerReader, requirementFactory, logger);
            Combat.Initialise(playerReader, requirementFactory, logger);
            Adhoc.Initialise(playerReader, requirementFactory, logger);
            Parallel.Initialise(playerReader, requirementFactory, logger);
            ShapeshiftForm.ForEach(i => i.Initialise(playerReader, requirementFactory, logger));

            Interact.Key = InteractKey;
            Interact.Initialise(playerReader, requirementFactory, logger);
            Blink.Initialise(playerReader, requirementFactory, logger);

            TargetLastTarget.Key = TargetLastTargetKey;
            TargetLastTarget.Initialise(playerReader, requirementFactory, logger);

            GatherFindKeys.ForEach(key =>
            {
                GatherFindKeyConfig.Add(new KeyAction { Key = key });
                GatherFindKeyConfig.Last().Initialise(playerReader, requirementFactory, logger);
            });
        }
    }
}