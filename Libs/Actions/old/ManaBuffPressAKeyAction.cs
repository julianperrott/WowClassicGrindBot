//using Libs.GOAP;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Text;
//using System.Threading.Tasks;

//namespace Libs.Actions
//{
//    public class ManaBuffPressAKeyAction : GoapAction
//    {
//        private readonly WowProcess wowProcess;
//        private readonly StopMoving stopMoving;
//        private readonly PlayerReader playerReader;
//        private ILogger logger;
//        private readonly Func<Task> actionBuff;
//        private readonly int secondsCooldown = 30;
//        private readonly string description;
//        private readonly Func<bool> hasDesiredBuff;
//        private readonly int manaPercentage;
//        private DateTime LastPressed = DateTime.Now.AddDays(-1);

//        public ManaBuffPressAKeyAction(WowProcess wowProcess, PlayerReader playerReader, StopMoving stopMoving, Func<Task> actionBuff, Func<bool> hasDesiredBuff, int manaPercentage, ILogger logger, string description)
//        {
//            this.wowProcess = wowProcess;
//            this.stopMoving = stopMoving;
//            this.description = description;
//            this.playerReader = playerReader;
//            this.actionBuff = actionBuff;
//            this.hasDesiredBuff = hasDesiredBuff;
//            this.manaPercentage = manaPercentage;
//            this.logger = logger;

//            AddPrecondition(GoapKey.incombat, false);
//        }

//        public override float CostOfPerformingAction { get => 1f; }

//        public override async Task PerformAction()
//        {
//            await this.stopMoving.Stop();

//            if (playerReader.PlayerBitValues.IsMounted)
//            {
//                await wowProcess.Dismount();
//            }

//            await Task.Delay(1000);

//            await actionBuff();

//            for (int i = 0; i < 12; i++)
//            {
//                if (HasDesiredBuff || this.playerReader.PlayerBitValues.PlayerInCombat)
//                {
//                    if (i > 5)
//                    {
//                        await wowProcess.TapStopKey();
//                    }

//                    this.logger.LogInformation($"I have the buff '{this.description}'");
//                    break;
//                }
//                await Task.Delay(1000);
//                this.logger.LogInformation($"Waiting for buff '{this.description}'");
//            }

//            if (HasDesiredBuff)
//            {
//                LastPressed = DateTime.Now;
//            }
//            else
//            {
//                // we should have got the buff, but perhaps we have run out, so don't try again for a while.
//                LastPressed = DateTime.Now.AddMinutes(10);
//            }
//        }

//        public bool HasEnoughMana => this.playerReader.ManaPercentage > manaPercentage;
//        public bool HasDesiredBuff => hasDesiredBuff();

//        public override bool CheckIfActionCanRun()
//        {
//            return (DateTime.Now - LastPressed).TotalSeconds > secondsCooldown && this.playerReader.ManaPercentage > manaPercentage && !HasDesiredBuff;
//        }

//        public override string Description()
//        {
//            if (HasDesiredBuff)
//            {
//                return $" - {description} - Has buff";
//            }
//            else
//            {
//                var timespan = LastPressed.AddSeconds(secondsCooldown) - DateTime.Now;
//                var timeCoolDownText = !CheckIfActionCanRun() ? DateTime.Now.Date.AddSeconds(timespan.TotalSeconds).ToString("mm:ss") : string.Empty;
//                var hasEnoughManaText = HasEnoughMana ? string.Empty : "Mana low";
//                var hasDesiredBuffText = HasDesiredBuff ? "Has buff" : "Buff needed";
//                return $" - {description} - {hasDesiredBuffText} {hasEnoughManaText} {timeCoolDownText}".Replace(" ", " ");
//            }
//        }
//    }
//}
