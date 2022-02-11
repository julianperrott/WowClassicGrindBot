using Microsoft.Extensions.Logging;
using System;
using Game;

namespace Core
{
    public class ConfigurableInput : WowProcessInput
    {
        private readonly bool log = true;

        public ClassConfiguration ClassConfig { private set; get; }

        public readonly int defaultKeyPress = 50;

        public readonly ConsoleKey ForwardKey;
        public readonly ConsoleKey BackwardKey;
        public readonly ConsoleKey TurnLeftKey;
        public readonly ConsoleKey TurnRightKey;

        public ConfigurableInput(ILogger logger, WowProcess wowProcess, ClassConfiguration classConfig) : base(logger, wowProcess)
        {
            ClassConfig = classConfig;

            ForwardKey = classConfig.ForwardKey;
            BackwardKey = classConfig.BackwardKey;
            TurnLeftKey = classConfig.TurnLeftKey;
            TurnRightKey = classConfig.TurnRightKey;

            if (log)
            {
                logger.LogInformation($"[{nameof(ConfigurableInput)}] Movement Keys. Forward: {ForwardKey} - Backward: {BackwardKey} - TurnLeft: {TurnLeftKey} - TurnRight: {TurnRightKey}");
            }
        }

        public void TapStopKey(string desc = "")
        {
            KeyPress(ForwardKey, defaultKeyPress, log ? $"TapStopKey: {desc}" : "");
        }

        public void TapInteractKey(string source)
        {
            KeyPress(ClassConfig.Interact.ConsoleKey, defaultKeyPress, log && string.IsNullOrEmpty(source) ? "" : $"TapInteract ({source})");
            this.ClassConfig.Interact.SetClicked();
        }

        public void TapApproachKey(string source)
        {
            KeyPress(ClassConfig.Approach.ConsoleKey, ClassConfig.Approach.PressDuration, log && string.IsNullOrEmpty(source) ? "" : $"TapApproachKey ({source})");
            this.ClassConfig.Approach.SetClicked();
        }

        public void TapLastTargetKey(string source)
        {
            KeyPress(ClassConfig.TargetLastTarget.ConsoleKey, defaultKeyPress, log ? $"TapLastTarget ({source})" : "");
            this.ClassConfig.TargetLastTarget.SetClicked();
        }

        public void TapStandUpKey(string desc = "")
        {
            KeyPress(ClassConfig.StandUp.ConsoleKey, defaultKeyPress, log ? $"TapStandUpKey: {desc}" : "");
            this.ClassConfig.StandUp.SetClicked();
        }

        public void TapClearTarget(string desc = "")
        {
            KeyPress(ClassConfig.ClearTarget.ConsoleKey, defaultKeyPress, log && string.IsNullOrEmpty(desc) ? "" : $"TapClearTarget: {desc}");
            this.ClassConfig.ClearTarget.SetClicked();
        }

        public void TapStopAttack(string desc = "")
        {
            KeyPress(ClassConfig.StopAttack.ConsoleKey, ClassConfig.StopAttack.PressDuration, log && string.IsNullOrEmpty(desc) ? "" : $"TapStopAttack: {desc}");
            this.ClassConfig.StopAttack.SetClicked();
        }

        public void TapNearestTarget(string desc = "")
        {
            KeyPress(ClassConfig.TargetNearestTarget.ConsoleKey, defaultKeyPress, log ? $"TapNearestTarget: {desc}" : "");
            this.ClassConfig.TargetNearestTarget.SetClicked();
        }

        public void TapTargetPet(string desc = "")
        {
            KeyPress(ClassConfig.TargetPet.ConsoleKey, defaultKeyPress, log ? $"TapTargetPet: {desc}" : "");
            this.ClassConfig.TargetPet.SetClicked();
        }

        public void TapTargetOfTarget(string desc = "")
        {
            KeyPress(ClassConfig.TargetTargetOfTarget.ConsoleKey, defaultKeyPress, log ? $"TapTargetsTarget: {desc}" : "");
            this.ClassConfig.TargetTargetOfTarget.SetClicked();
        }

        public void TapJump(string desc = "")
        {
            KeyPress(ClassConfig.Jump.ConsoleKey, defaultKeyPress, log ? $"TapJump: {desc}" : "");
            this.ClassConfig.Jump.SetClicked();
        }

        public void TapPetAttack(string source = "")
        {
            KeyPress(ClassConfig.PetAttack.ConsoleKey, ClassConfig.PetAttack.PressDuration, log ? $"TapPetAttack ({source})" : "");
            this.ClassConfig.PetAttack.SetClicked();
        }

        public void TapHearthstone()
        {
            KeyPress(ConsoleKey.I, defaultKeyPress, log ? "TapHearthstone" : "");
        }

        public void TapMount()
        {
            KeyPress(ClassConfig.Mount.ConsoleKey, defaultKeyPress, log ? "TapMount" : "");
            this.ClassConfig.Mount.SetClicked();
        }

        public void TapDismount()
        {
            KeyPress(ClassConfig.Mount.ConsoleKey, defaultKeyPress, log ? "TapDismount" : "");
        }
    }
}
