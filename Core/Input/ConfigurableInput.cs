using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Game;

namespace Core
{
    public class ConfigurableInput : WowProcessInput
    {
        public ClassConfiguration ClassConfig { private set; get; }

        private const int defaultKeyPress = 50;

        public ConfigurableInput(ILogger logger, WowProcess wowProcess, ClassConfiguration classConfig) : base(logger, wowProcess)
        {
            ClassConfig = classConfig;
        }

        public async Task TapStopKey(string desc = "")
        {
            await KeyPress(ConsoleKey.UpArrow, defaultKeyPress, $"TapStopKey: {desc}");
        }

        public async Task TapInteractKey(string source)
        {
            await KeyPress(ClassConfig.Interact.ConsoleKey, defaultKeyPress, string.IsNullOrEmpty(source) ? "" : $"TapInteract ({source})");
            this.ClassConfig.Interact.SetClicked();
        }

        public async Task TapLastTargetKey(string source)
        {
            await KeyPress(ClassConfig.TargetLastTarget.ConsoleKey, defaultKeyPress, $"TapLastTarget ({source})");
            this.ClassConfig.TargetLastTarget.SetClicked();
        }

        public async Task TapStandUpKey(string desc = "")
        {
            await KeyPress(ClassConfig.StandUp.ConsoleKey, defaultKeyPress, $"TapStandUpKey: {desc}");
            this.ClassConfig.StandUp.SetClicked();
        }

        public async Task TapClearTarget(string desc = "")
        {
            await KeyPress(ClassConfig.ClearTarget.ConsoleKey, defaultKeyPress, string.IsNullOrEmpty(desc) ? "" : $"TapClearTarget: {desc}");
            this.ClassConfig.ClearTarget.SetClicked();
        }

        public async Task TapStopAttack(string desc = "")
        {
            await KeyPress(ClassConfig.StopAttack.ConsoleKey, defaultKeyPress, string.IsNullOrEmpty(desc) ? "" : $"TapStopAttack: {desc}");
            this.ClassConfig.StopAttack.SetClicked();
        }

        public async Task TapNearestTarget(string desc = "")
        {
            await KeyPress(ClassConfig.TargetNearestTarget.ConsoleKey, defaultKeyPress, $"TapNearestTarget: {desc}");
            this.ClassConfig.TargetNearestTarget.SetClicked();
        }

        public async Task TapTargetPet(string desc = "")
        {
            await KeyPress(ClassConfig.TargetPet.ConsoleKey, defaultKeyPress, $"TapTargetPet: {desc}");
            this.ClassConfig.TargetPet.SetClicked();
        }

        public async Task TapTargetOfTarget(string desc = "")
        {
            await KeyPress(ClassConfig.TargetTargetOfTarget.ConsoleKey, defaultKeyPress, $"TapTargetsTarget: {desc}");
            this.ClassConfig.TargetTargetOfTarget.SetClicked();
        }

        public async Task TapJump(string desc = "")
        {
            await KeyPress(ClassConfig.Jump.ConsoleKey, defaultKeyPress, $"TapJump: {desc}");
            this.ClassConfig.Jump.SetClicked();
        }

        public async Task TapPetAttack(string source = "")
        {
            await KeyPress(ClassConfig.PetAttack.ConsoleKey, defaultKeyPress, $"TapPetAttack ({source})");
            this.ClassConfig.PetAttack.SetClicked();
        }

        public async Task TapHearthstone()
        {
            await KeyPress(ConsoleKey.I, defaultKeyPress, "TapHearthstone");
        }

        public async Task TapMount()
        {
            await KeyPress(ClassConfig.Mount.ConsoleKey, defaultKeyPress, "TapMount");
            this.ClassConfig.Mount.SetClicked();
        }

        public async Task TapDismount()
        {
            await KeyPress(ClassConfig.Mount.ConsoleKey, defaultKeyPress, "TapDismount");
        }
    }
}
