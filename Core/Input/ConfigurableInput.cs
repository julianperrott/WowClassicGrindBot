using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using SharedLib;
using Game;

namespace Core
{
    public class ConfigurableInput : WowProcessInput
    {
        private readonly ILogger logger;
        private readonly ClassConfiguration classConfig;

        private const int defaultKeyPress = 50;

        private bool debug = false;

        public ConfigurableInput(ILogger logger, WowProcess wowProcess, ClassConfiguration classConfiguration) : base(logger, wowProcess)
        {
            this.logger = logger;
            this.classConfig = classConfiguration;
        }

        public async Task TapStopKey(string desc = "")
        {
            Log($"TapStopKey: {desc}");
            await KeyPress(ConsoleKey.UpArrow, defaultKeyPress);
        }

        public async Task TapInteractKey(string source)
        {
            Log($"TapInteract ({source})");
            await KeyPress(classConfig.Interact.ConsoleKey, defaultKeyPress);
            this.classConfig.Interact.SetClicked();
        }

        public async Task TapLastTargetKey(string source)
        {
            Log($"TapLastTarget ({source})");
            await KeyPress(classConfig.TargetLastTarget.ConsoleKey, defaultKeyPress);
            this.classConfig.TargetLastTarget.SetClicked();
        }

        public async Task TapStandUpKey(string desc = "")
        {
            Log($"TapStandUpKey: {desc}");
            await KeyPress(classConfig.StandUp.ConsoleKey, defaultKeyPress);
            this.classConfig.StandUp.SetClicked();
        }

        public async Task TapClearTarget(string desc = "")
        {
            Log($"TapClearTarget: {desc}");
            await KeyPress(classConfig.ClearTarget.ConsoleKey, defaultKeyPress);
            this.classConfig.ClearTarget.SetClicked();
        }

        public async Task TapStopAttack(string desc = "")
        {
            Log($"TapStopAttack: {desc}");
            await KeyPress(classConfig.StopAttack.ConsoleKey, defaultKeyPress);
            this.classConfig.StopAttack.SetClicked();
        }

        public async Task TapNearestTarget(string desc = "")
        {
            Log($"TapNearestTarget: {desc}");
            await KeyPress(classConfig.TargetNearestTarget.ConsoleKey, defaultKeyPress);
            this.classConfig.TargetNearestTarget.SetClicked();
        }

        public async Task TapTargetPet(string desc = "")
        {
            Log($"TapTargetPet: {desc}");
            await KeyPress(classConfig.TargetPet.ConsoleKey, defaultKeyPress);
            this.classConfig.TargetPet.SetClicked();
        }

        public async Task TapTargetOfTarget(string desc = "")
        {
            Log($"TapTargetsTarget: {desc}");
            await KeyPress(classConfig.TargetTargetOfTarget.ConsoleKey, defaultKeyPress);
            this.classConfig.TargetTargetOfTarget.SetClicked();
        }

        public async Task TapJump(string desc = "")
        {
            Log($"TapJump: {desc}");
            await KeyPress(classConfig.Jump.ConsoleKey, defaultKeyPress);
            this.classConfig.Jump.SetClicked();
        }

        public async Task TapPetAttack(string source = "")
        {
            Log($"TapPetAttack ({source})");
            await KeyPress(classConfig.PetAttack.ConsoleKey, defaultKeyPress);
            this.classConfig.PetAttack.SetClicked();
        }

        public async Task Hearthstone()
        {
            // hearth macro = /use hearthstone
            await KeyPress(ConsoleKey.I, defaultKeyPress);
        }

        public async Task Mount(PlayerReader playerReader)
        {
            await KeyPress(ConsoleKey.O, defaultKeyPress);

            for (int i = 0; i < 40; i++)
            {
                if (playerReader.PlayerBitValues.IsMounted) { return; }
                if (playerReader.PlayerBitValues.PlayerInCombat) { return; }
                await Task.Delay(100);
            }
        }

        public async Task Dismount()
        {
            await KeyPress(ConsoleKey.O, defaultKeyPress);
            await Task.Delay(1500);
        }


        private void Log(string text)
        {
            if (debug)
                logger.LogInformation($"{this.GetType().Name}: {text}");
        }

    }
}
