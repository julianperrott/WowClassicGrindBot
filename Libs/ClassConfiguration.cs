using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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

        public KeyConfigurations Pull { get; set; } = new KeyConfigurations();
        public KeyConfigurations Combat { get; set; } = new KeyConfigurations();
        public KeyConfigurations Adhoc { get; set; } = new KeyConfigurations();

        public List<KeyConfiguration> ShapeshiftForm { get; set; } = new List<KeyConfiguration>();

        public static Dictionary<ShapeshiftForm, ConsoleKey> ShapeshiftFormKeys = new Dictionary<ShapeshiftForm, ConsoleKey>();

        public void Initialise(PlayerReader playerReader, ILogger logger)
        {
            Pull.Initialise(playerReader, logger);
            Combat.Initialise(playerReader, logger);
            Adhoc.Initialise(playerReader, logger);
            ShapeshiftForm.ForEach(i => i.Initialise(playerReader, logger));
        }
    }
    public class KeyConfigurations
    {
        public List<KeyConfiguration> Sequence { get; set; } = new List<KeyConfiguration>();

        public void Initialise(PlayerReader playerReader, ILogger logger)
        {
            Sequence.ForEach(i => i.Initialise(playerReader, logger));
        }
    }

    public class KeyConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public bool HasCastBar { get; set; }
        public bool StopBeforeCast { get; set; } = false;
        public ConsoleKey ConsoleKey { get; set; } = 0;
        public string Key { get; set; } = string.Empty;
        public int PressDuration { get; set; } = 200;
        public string ShapeShiftForm { get; set; } = string.Empty;
        public ShapeshiftForm ShapeShiftFormEnum { get; set; } = ShapeshiftForm.None;
        public string CastIfAddsVisible { get; set; } = "";
        public int Cooldown { get; set; } = 0;
        public int MinMana { get; set; } = 0;
        public int MinComboPoints { get; set; } = 0;
        public string Requirement { get; set; } = string.Empty;
        public bool WaitForWithinMelleRange { get; set; } = false;
        public bool ResetOnNewTarget { get; set; } = false;

        public bool Log { get; set; } = true;
        public int DelayAfterCast { get; set; } = 1500;
        public float Cost { get; set; } = 18;
        public string InCombat { get; set; } = "false";

        public PlayerReader.Requirement? RequirementObject { get; set; }

        public void Initialise(PlayerReader playerReader, ILogger logger)
        {
            if (RequirementObject == null)
            {
                RequirementObject = playerReader.GetRequirement(this);
            }

            if (!string.IsNullOrEmpty(ShapeShiftForm))
            {
                if (Enum.TryParse(typeof(ShapeshiftForm), ShapeShiftForm, out var desiredForm))
                {
                    this.ShapeShiftFormEnum = (ShapeshiftForm)desiredForm;
                }
                else
                {
                    logger.LogInformation($"Unknown shapeshift form: {ShapeShiftForm}");
                }
            }

            ReadKey(logger);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Requirement))
            {
                return $"{Name} [{ConsoleKey.ToString()}] - ";
            }

            var status = "NEED";
            if (RequirementObject != null)
            {
                status = RequirementObject.HasRequirement() ? "OK" : "NEED";
            }

            if (Name.Contains(Requirement))
            {
                return $"{Name} [{ConsoleKey.ToString()}] - {status}";
            }
            else
            {
                return $"{Name} [{ConsoleKey.ToString()}] ({Requirement}) - {status}";
            }
        }

        public Dictionary<string, ConsoleKey> KeyMapping = new Dictionary<string, ConsoleKey>()
        {
            {"0",ConsoleKey.D0 },
            {"1",ConsoleKey.D1 },
            {"2",ConsoleKey.D2 },
            {"3",ConsoleKey.D3 },
            {"4",ConsoleKey.D4 },
            {"5",ConsoleKey.D5 },
            {"6",ConsoleKey.D6 },
            {"7",ConsoleKey.D7 },
            {"8",ConsoleKey.D8 },
            {"9",ConsoleKey.D9 },
            {"-",ConsoleKey.OemMinus },
            {"=",ConsoleKey.OemPlus },
        };

        public bool ReadKey(ILogger logger)
        {
            if (string.IsNullOrEmpty(Key))
            {
                logger.LogError($"You must specify either 'Key' (ConsoleKey value) or 'KeyName' (ConsoleKey enum name) for { this.Name}");
                return false;
            }

            if (KeyMapping.ContainsKey(this.Key))
            {
                this.ConsoleKey = KeyMapping[this.Key];
            }
            else
            {
                var consoleKey = ((Enum.GetValues(typeof(ConsoleKey))) as IEnumerable<ConsoleKey>)
                    .FirstOrDefault(k => k.ToString() == this.Key);

                if (consoleKey == 0)
                {
                    logger.LogError($"You must specify a valid 'KeyName' (ConsoleKey enum name) for { this.Name}");
                    return false;
                }

                this.ConsoleKey = consoleKey;
            }

            return true;
        }
    }
}
