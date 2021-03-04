using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Libs
{
    public class WowInput
    {
        private readonly ILogger logger;
        private readonly WowProcess wowProcess;
        private readonly ClassConfiguration classConfig;

        private const int defaultKeyPress = 75;

        private bool debug = false;

        public WowInput(ILogger logger, WowProcess wowProcess, ClassConfiguration classConfiguration)
        {
            this.logger = logger;
            this.wowProcess = wowProcess;
            this.classConfig = classConfiguration;
        }

        public async Task TapStopKey(string desc = "")
        {
            Log($"TapStopKey: {desc}");
            await wowProcess.KeyPress(ConsoleKey.UpArrow, defaultKeyPress);
        }

        public async Task TapInteractKey(string source)
        {
            Log($"TapInteract ({source})");
            await wowProcess.KeyPress(classConfig.Interact.ConsoleKey, defaultKeyPress);
            this.classConfig.Interact.SetClicked();
        }

        public async Task TapLastTargetKey(string source)
        {
            Log($"TapLastTarget ({source})");
            await wowProcess.KeyPress(classConfig.TargetLastTarget.ConsoleKey, defaultKeyPress);
            this.classConfig.TargetLastTarget.SetClicked();
        }

        public async Task TapStandUpKey(string desc = "")
        {
            Log($"TapStandUpKey: {desc}");
            await wowProcess.KeyPress(classConfig.StandUp.ConsoleKey, defaultKeyPress);
            this.classConfig.StandUp.SetClicked();
        }

        public async Task TapClearTarget(string desc = "")
        {
            Log($"TapClearTarget: {desc}");
            await wowProcess.KeyPress(classConfig.ClearTarget.ConsoleKey, defaultKeyPress);
            this.classConfig.ClearTarget.SetClicked();
        }

        public async Task TapStopAttack(string desc = "")
        {
            Log($"TapStopAttack: {desc}");
            await wowProcess.KeyPress(classConfig.StopAttack.ConsoleKey, defaultKeyPress);
            this.classConfig.StopAttack.SetClicked();
        }

        public async Task TapNearestTarget(string desc = "")
        {
            Log($"TapNearestTarget: {desc}");
            await wowProcess.KeyPress(classConfig.TargetNearestTarget.ConsoleKey, defaultKeyPress);
            this.classConfig.TargetNearestTarget.SetClicked();
        }

        public async Task TapTargetPet(string desc = "")
        {
            Log($"TapTargetPet: {desc}");
            await wowProcess.KeyPress(classConfig.TargetPet.ConsoleKey, defaultKeyPress);
            this.classConfig.TargetPet.SetClicked();
        }

        public async Task TapTargetOfTarget(string desc = "")
        {
            Log($"TapTargetsTarget: {desc}");
            await wowProcess.KeyPress(classConfig.TargetTargetOfTarget.ConsoleKey, defaultKeyPress);
            this.classConfig.TargetTargetOfTarget.SetClicked();
        }

        public async Task TapJump(string desc = "")
        {
            Log($"TapJump: {desc}");
            await wowProcess.KeyPress(classConfig.Jump.ConsoleKey, defaultKeyPress);
            this.classConfig.Jump.SetClicked();
        }


        public async Task Hearthstone()
        {
            // hearth macro = /use hearthstone
            await wowProcess.KeyPress(ConsoleKey.I, defaultKeyPress);
        }

        public async Task Mount(PlayerReader playerReader)
        {
            await wowProcess.KeyPress(ConsoleKey.O, defaultKeyPress);

            for (int i = 0; i < 40; i++)
            {
                if (playerReader.PlayerBitValues.IsMounted) { return; }
                if (playerReader.PlayerBitValues.PlayerInCombat) { return; }
                await Task.Delay(100);
            }
        }

        public async Task Dismount()
        {
            await wowProcess.KeyPress(ConsoleKey.O, defaultKeyPress);
            await Task.Delay(1500);
        }


        private void Log(string text)
        {
            if (debug)
                logger.LogInformation($"{this.GetType().Name}: {text}");
        }

    }
}
