using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Libs
{
    public class KeyConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public bool HasCastBar { get; set; }
        public bool StopBeforeCast { get; set; } = false;
        public ConsoleKey ConsoleKey { get; set; } = 0;
        public string Key { get; set; } = string.Empty;
        public int PressDuration { get; set; } = 250;
        public string ShapeShiftForm { get; set; } = string.Empty;
        public ShapeshiftForm ShapeShiftFormEnum { get; set; } = ShapeshiftForm.None;
        public string CastIfAddsVisible { get; set; } = "";
        public int Cooldown { get; set; } = 0;

        public int MinMana { get; set; } = 0;
        public int MinRage { get; set; } = 0;
        public int MinEnergy { get; set; } = 0;
        public int MinComboPoints { get; set; } = 0;

        public string Requirement { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new List<string>();

        public bool WaitForWithinMelleRange { get; set; } = false;
        public bool ResetOnNewTarget { get; set; } = false;

        public bool Log { get; set; } = true;
        public int DelayAfterCast { get; set; } = 1500;
        public int DelayBeforeCast { get; set; } = 0;
        public float Cost { get; set; } = 18;
        public string InCombat { get; set; } = "false";

        public WowPoint LastClickPostion { get; private set; } = new WowPoint(0, 0);

        public List<Requirement> RequirementObjects { get; set; } = new List<Requirement>();

        protected static Dictionary<ConsoleKey, DateTime> LastClicked = new Dictionary<ConsoleKey, DateTime>();

        private PlayerReader? playerReader;

        private ILogger? logger;

        public void Initialise(PlayerReader playerReader, RequirementFactory requirementFactory, ILogger logger)
        {
            this.playerReader = playerReader;
            this.logger = logger;

            if (!string.IsNullOrEmpty(this.Requirement))
            {
                Requirements.Add(this.Requirement);
            }

            requirementFactory.InitialiseRequirements(this);

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

            KeyReader.ReadKey(logger, this);
        }

        public void CreateCooldownRequirement()
        {
            if (this.Cooldown > 0)
            {
                this.RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => GetCooldownRemaining() == 0,
                    LogMessage = () => $"Cooldown {GetCooldownRemaining()}",
                    VisibleIfHasRequirement = false
                });
            }
        }

        public int GetCooldownRemaining()
        {
            if (!LastClicked.ContainsKey(this.ConsoleKey))
            {
                return 0;
            }

            var remaining = this.Cooldown - ((int)(DateTime.Now - LastClicked[this.ConsoleKey]).TotalSeconds);

            return remaining < 0 ? 0 : remaining;
        }

        internal void SetClicked()
        {
            if (this.playerReader != null)
            {
                LastClickPostion = this.playerReader.PlayerLocation;
            }

            if (LastClicked.ContainsKey(this.ConsoleKey))
            {
                LastClicked[this.ConsoleKey] = DateTime.Now;
            }
            else
            {
                LastClicked.Add(this.ConsoleKey, DateTime.Now);
            }
        }

        public double SecondsSinceLastClick => LastClicked.ContainsKey(this.ConsoleKey) ? (DateTime.Now - LastClicked[this.ConsoleKey]).TotalSeconds : 999;

        internal void ResetCooldown()
        {
            if (LastClicked.ContainsKey(ConsoleKey)) { LastClicked.Remove(ConsoleKey); }
        }

        public bool CanRun()
        {
            return !this.RequirementObjects.Any(r => !r.HasRequirement());
        }

        public void LogInformation(string message)
        {
            if (this.Log)
            {
                logger.LogInformation($"{this.Name}: {message}");
            }
        }
    }
}