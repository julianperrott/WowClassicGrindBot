using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class KeyAction
    {
        public string Name { get; set; } = string.Empty;
        public bool HasCastBar { get; set; }
        public bool StopBeforeCast { get; set; } = false;
        public ConsoleKey ConsoleKey { get; set; } = 0;
        public string Key { get; set; } = string.Empty;
        public int PressDuration { get; set; } = 50;
        public string Form { get; set; } = string.Empty;
        public Form FormEnum { get; set; } = Core.Form.None;
        public string CastIfAddsVisible { get; set; } = "";
        public float Cooldown { get; set; } = 0;

        private int _charge;
        public int Charge { get; set; } = 1;
        public SchoolMask School { get; set; } = SchoolMask.None;
        public int MinMana { get; set; } = 0;
        public int MinRage { get; set; } = 0;
        public int MinEnergy { get; set; } = 0;
        public int MinComboPoints { get; set; } = 0;

        public string Requirement { get; set; } = string.Empty;
        public List<string> Requirements { get; } = new List<string>();

        public bool WhenUsable { get; set; } = false;

        public bool WaitForWithinMeleeRange { get; set; } = false;
        public bool ResetOnNewTarget { get; set; } = false;

        public bool Log { get; set; } = true;
        public int DelayAfterCast { get; set; } = 1450; // GCD 1500 - but spell queue window 400 ms

        public bool WaitForGCD { get; set; } = true;

        public bool AfterCastWaitBuff = false;

        public bool AfterCastWaitNextSwing = false;

        public bool DelayUntilCombat { get; set; } = false;
        public int DelayBeforeCast { get; set; } = 0;
        public float Cost { get; set; } = 18;
        public string InCombat { get; set; } = "false";

        public bool? UseWhenTargetIsCasting { get; set; }

        public string PathFilename { get; set; } = string.Empty;
        public List<WowPoint> Path { get; } = new List<WowPoint>();

        public int StepBackAfterCast {get; set; } = 0;

        public WowPoint LastClickPostion { get; private set; } = new WowPoint(0, 0);

        public List<Requirement> RequirementObjects { get; } = new List<Requirement>();

        public int ConsoleKeyFormHash;

        protected static ConcurrentDictionary<int, DateTime> LastClicked { get; } = new ConcurrentDictionary<int, DateTime>();

        public static int LastKeyClicked()
        {
            var last = LastClicked.OrderByDescending(s => s.Value).FirstOrDefault();
            if (last.Key == 0 || (DateTime.Now - last.Value).TotalSeconds > 2)
            {
                return (int)ConsoleKey.NoName;
            }
            return last.Key;
        }

        private PlayerReader? playerReader;

        private ILogger? logger;

        public void Initialise(AddonReader addonReader, RequirementFactory requirementFactory, ILogger logger)
        {
            this.playerReader = addonReader.PlayerReader;
            this.logger = logger;

            ResetCharges();

            KeyReader.ReadKey(logger, this);

            if (!string.IsNullOrEmpty(this.Requirement))
            {
                Requirements.Add(this.Requirement);
            }

            if (!string.IsNullOrEmpty(Form))
            {
                if (Enum.TryParse(typeof(Form), Form, out var desiredForm))
                {
                    this.FormEnum = (Form)desiredForm;
                    this.logger.LogInformation($"[{Name}] Required Form: {FormEnum}");

                    if (KeyReader.ActionBarSlotMap.TryGetValue(Key, out int slot))
                    {
                        int offset = Stance.FormToActionBar(playerReader.PlayerClass, FormEnum);
                        this.logger.LogInformation($"[{Name}] Actionbar Form key map: Key:{Key} -> Actionbar:{slot} -> Form Map:{slot + offset}");
                    }
                }
                else
                {
                    logger.LogInformation($"Unknown form: {Form}");
                }
            }

            ConsoleKeyFormHash = ((int)FormEnum * 1000) + (int)ConsoleKey;

            UpdateMinResourceRequirement(playerReader, addonReader.ActionBarCostReader);

            requirementFactory.InitialiseRequirements(this);
        }

        public void InitialiseForm(AddonReader addonReader, RequirementFactory requirementFactory, ILogger logger)
        {
            Initialise(addonReader, requirementFactory, logger);

            if (!string.IsNullOrEmpty(Form))
            {
                if (addonReader.PlayerReader.FormCost.ContainsKey(FormEnum))
                {
                    addonReader.PlayerReader.FormCost.Remove(FormEnum);
                }

                addonReader.PlayerReader.FormCost.Add(FormEnum, MinMana);
                LogInformation($"Added {FormEnum} to FormCost with {MinMana}");
            }
        }

        public void CreateCooldownRequirement()
        {
            if (this.Cooldown > 0)
            {
                this.RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => GetCooldownRemaining() == 0,
                    LogMessage = () => $"Cooldown {GetCooldownRemaining() / 1000:F1}",
                    VisibleIfHasRequirement = false
                });
            }
        }

        public float GetCooldownRemaining()
        {
            try
            {
                if (!LastClicked.ContainsKey(ConsoleKeyFormHash))
                {
                    return 0;
                }

                var remaining = this.Cooldown - ((int)(DateTime.Now - LastClicked[ConsoleKeyFormHash]).TotalMilliseconds);

                return remaining < 0 ? 0 : remaining;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "GetCooldownRemaining()");
                return 0;
            }
        }

        internal void SetClicked()
        {
            try
            {
                if (this.playerReader != null)
                {
                    LastClickPostion = this.playerReader.PlayerLocation;
                }

                if (LastClicked.ContainsKey(ConsoleKeyFormHash))
                {
                    LastClicked[ConsoleKeyFormHash] = DateTime.Now;
                }
                else
                {
                    LastClicked.TryAdd(ConsoleKeyFormHash, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "SetClicked()");
            }
        }

        public double MillisecondsSinceLastClick => LastClicked.ContainsKey(ConsoleKeyFormHash) ? (DateTime.Now - LastClicked[ConsoleKeyFormHash]).TotalMilliseconds : double.MaxValue;

        internal void ResetCooldown()
        {
            if (LastClicked.ContainsKey(ConsoleKeyFormHash))
            {
                LastClicked.TryRemove(ConsoleKeyFormHash, out _);
            }
        }

        public void CreateChargeRequirement()
        {
            if (this.Charge > 1)
            {
                this.RequirementObjects.Add(new Requirement
                {
                    HasRequirement = () => GetChargeRemaining() != 0,
                    LogMessage = () => $"Charge {GetChargeRemaining()}",
                    VisibleIfHasRequirement = true
                });
            }
        }

        public int GetChargeRemaining()
        {
            return _charge;
        }

        public void ConsumeCharge()
        {
            if(Charge > 1)
            {
                _charge--;
                if(_charge > 0)
                {
                    ResetCooldown();
                }
                else
                {
                    ResetCharges();
                    SetClicked();
                }
            }
        }

        internal void ResetCharges()
        {
            _charge = Charge;
        }

        public bool CanRun()
        {
            return !this.RequirementObjects.Any(r => !r.HasRequirement());
        }

        private void UpdateMinResourceRequirement(PlayerReader playerReader, ActionBarCostReader actionBarCostReader)
        {
            var tuple = actionBarCostReader.GetCostByActionBarSlot(playerReader, this);
            if (tuple.Item2 != 0)
            {
                int oldValue = 0;
                switch (tuple.Item1)
                {
                    case PowerType.Mana:
                        oldValue = MinMana;
                        MinMana = tuple.Item2;
                        break;
                    case PowerType.Rage:
                        oldValue = MinRage;
                        MinRage = tuple.Item2;
                        break;
                    case PowerType.Energy:
                        oldValue = MinEnergy;
                        MinEnergy = tuple.Item2;
                        break;
                }

                logger.LogInformation($"[{Name}] Update {tuple.Item1} cost to {tuple.Item2} from {oldValue}");
            }
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