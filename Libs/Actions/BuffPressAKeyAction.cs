using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class BuffPressAKeyAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly StopMoving stopMoving;
        private readonly PlayerReader playerReader;
        private ILogger logger;
        private readonly ConsoleKey key;
        private readonly int secondsCooldown = 30;
        private readonly string description;
        private readonly Func<bool> hasDesiredBuff;
        private DateTime LastPressed = DateTime.Now.AddDays(-1);

        public BuffPressAKeyAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, ConsoleKey key, Func<bool> hasDesiredBuff, ILogger logger, string description)
        {
            this.wowProcess = wowProcess;
            this.stopMoving = stopMoving;
            this.description = description;
            this.playerReader = playerReader;
            this.key = key;
            this.hasDesiredBuff = hasDesiredBuff;
            this.logger = logger;

            AddPrecondition(GoapKey.incombat, false);
        }

        public override float CostOfPerformingAction { get => 1f; }

        public override async Task PerformAction()
        {
            await this.stopMoving.Stop();

            await wowProcess.KeyPress(key, 500);

            for (int i = 0; i < 12; i++)
            {
                if (HasDesiredBuff || this.playerReader.PlayerBitValues.PlayerInCombat)
                {
                    break;
                }
                await Task.Delay(1000);
            }

            if (HasDesiredBuff)
            {
                LastPressed = DateTime.Now;
            }
            else
            {
                // we should have got the buff, but perhaps we have run out, so don't try again for a while.
                LastPressed = DateTime.Now.AddMinutes(10);
            }
        }

        public bool HasDesiredBuff => hasDesiredBuff();

        public override bool CheckIfActionCanRun()
        {
            return (DateTime.Now - LastPressed).TotalSeconds > secondsCooldown && !HasDesiredBuff;
        }

        public override string Description()
        {
            if (HasDesiredBuff)
            {
                return $" - {description} - Has buff";
            }
            else
            {
                var timespan = LastPressed.AddSeconds(secondsCooldown) - DateTime.Now;
                var timeCoolDownText = !CheckIfActionCanRun() ? DateTime.Now.Date.AddSeconds(timespan.TotalSeconds).ToString("mm:ss") : string.Empty;
                var hasDesiredBuffText = HasDesiredBuff ? "Has buff" : "Buff needed";
                return $" - {description} - {hasDesiredBuffText} {timeCoolDownText}".Replace(" ", " ");
            }
        }
    }
}
