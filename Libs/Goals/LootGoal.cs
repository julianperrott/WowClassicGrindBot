using Libs.GOAP;
using Libs.Looting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class LootGoal : GoapGoal
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly LootWheel lootWheel;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly ClassConfiguration classConfiguration;
        private ILogger logger;

        private bool debug = true;

        public LootGoal(WowProcess wowProcess, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;
            this.logger = logger;
            this.classConfiguration = classConfiguration;

            lootWheel = new LootWheel(wowProcess, playerReader, logger);
        }

        public virtual void AddPreconditions()
        {
            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.hastarget, false);
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }

        public override float CostOfPerformingAction { get => 4f; }


        private enum TargetResult
        { 
            InCombat,
            NoTarget,
            TargetNotTargetingMe,
        }


        private async Task<TargetResult> AmIBeingTargetted()
        {
            // check for targets attacking me
            await wowProcess.KeyPress(ConsoleKey.Tab, 100);

            await Task.Delay(300);

            if (this.playerReader.HasTarget)
            {
                if (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
                {
                    await this.TapInteractKey("LootAction");
                    return TargetResult.InCombat;
                }
                await wowProcess.KeyPress(ConsoleKey.F3, 200);
                return TargetResult.TargetNotTargetingMe;
            }
            return TargetResult.NoTarget;
        }

        private bool outOfCombat = false;

        public async Task<bool> CheckIfEnterredCombat()
        {
            if (!outOfCombat && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Left combat");
                outOfCombat = true;
            }

            if (this.playerReader.PlayerBitValues.PlayerInCombat && this.playerReader.PlayerClass == PlayerClassEnum.Warlock)
            {
                // /Target pet
                await wowProcess.KeyPress(ConsoleKey.F12, 300);

                if (this.playerReader.TargetTarget == TargetTargetEnum.PetHasATarget)
                {
                    await wowProcess.KeyPress(ConsoleKey.U, 200); // switch to pet's target /tar targettarget
                    return true;
                }
                else
                {
                    wowProcess.KeyPress(ConsoleKey.F3, 400).Wait(); // clear target
                }
            }

            if (outOfCombat && playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Combat detected");
                return true;
            }


            return false;
        }

        public override async Task PerformAction()
        {
            await wowProcess.KeyPress(ConsoleKey.F9, 100); // stand
            await stopMoving.Stop();

            // when the target is dead
            // the player lose the target
            // ?? wait till losing combat maximum 10 updates
            await WaitTilDropCombat(10);

            TargetResult targetResult = TargetResult.NoTarget;
            bool searchForMobs = playerReader.PlayerBitValues.PlayerInCombat;
            if (searchForMobs)
            {
                if(playerReader.PlayerBitValues.PlayerInCombat)
                {
                    targetResult = await AmIBeingTargetted();
                    if (targetResult == TargetResult.InCombat) { return; }
                }
            }

            searchForMobs = playerReader.PlayerBitValues.PlayerInCombat;
            var healthAtStartOfLooting = playerReader.HealthPercent;

            var lootAttempt = 0;
            while (lootAttempt < 10)
            {
                if (!this.classConfiguration.Loot && !playerReader.PlayerBitValues.PlayerInCombat)
                {
                    return;
                }

                // Fast loot - only applicabe for single mob combat
                // after leaving combat using 'targetlasttarget' targets the corpse
                // use 'interact' on him makes you run towards him and loot if close enough
                if (this.classConfiguration.Loot && lootAttempt == 0 && targetResult == TargetResult.NoTarget)
                {
                    WowPoint lastPosition = playerReader.PlayerLocation;
                    
                    await this.TapTargetLastTargetKey("lootAttempt 0 - fast loot");
                    await this.TapInteractKey("lootAttempt 0 - fast loot");

                    logger.LogInformation("wait till the player become stil!");
                    while (IsPlayerMoving(lastPosition))
                    {
                        lastPosition = playerReader.PlayerLocation;
                        await playerReader.WaitForNUpdate(1);
                        if (playerReader.PlayerBitValues.PlayerInCombat)
                            return;
                    }

                    // wait grabbing the loot
                    await playerReader.WaitForNUpdate(2);

                    logger.LogDebug("Fast Loot Successfull");
                    SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
                    SendActionEvent(new ActionEventArgs(GoapKey.postloot, false));
                    return;
                }

                if (await CheckIfEnterredCombat()) { return; }

                Log(searchForMobs ? "Searching for mobs" : $"Looting (attempt: {lootAttempt + 1}.");
                SendActionEvent(new ActionEventArgs(GoapKey.shouldloot, false));
                var foundSomething = await lootWheel.Loot(searchForMobs);

                if (foundSomething && lootWheel.Classification == Cursor.CursorClassification.Kill)
                {
                    await AquireTarget();
                    return;
                }

                if (this.playerReader.PlayerClass == PlayerClassEnum.Druid && this.playerReader.Druid_ShapeshiftForm!= ShapeshiftForm.None)
                {
                    var desiredFormKey = this.classConfiguration.ShapeshiftForm
                        .Where(s => s.ShapeShiftFormEnum == ShapeshiftForm.None)
                        .FirstOrDefault();
                    if (desiredFormKey!=null)
                    {
                        await this.wowProcess.KeyPress(desiredFormKey.ConsoleKey, 500, "Cancel form to allow drinking");
                    }

                }

                if (!foundSomething && lootAttempt>0)// && !searchForMobs)
                {
                    lootAttempt = 10;
                }
                else
                {
                    Log($"Found {lootWheel.Classification}");
                    if (searchForMobs)
                    {
                        searchForMobs = false;
                    }
                    else
                    {
                        if (lootWheel.Classification == Cursor.CursorClassification.Kill)
                        {
                            await this.TapInteractKey("LootAction");
                            Log($"Kill something !");
                            return;
                        }
                        else
                        {
                            if (healthAtStartOfLooting > playerReader.HealthPercent && playerReader.PlayerBitValues.PlayerInCombat)
                            {
                                Log($"Losing health and still in combat !");
                                return;
                            }

                            Log($"Looted {lootWheel.Classification}");
                        }
                    }
                }

                lootAttempt++;
            }
            SendActionEvent(new ActionEventArgs(GoapKey.postloot, true));
        }

        private async Task AquireTarget()
        {
            Log("We are being attacked!");

            for (int i = 0; i < 2000; i += 100)
            {
                Log("Waiting for target to be recognised!");
                await Task.Delay(100);
                if (string.IsNullOrEmpty(playerReader.Target) && playerReader.PlayerBitValues.TargetIsDead && playerReader.TargetHealth == 0)
                {
                    await Task.Delay(100);
                    Log($"Waiting for target to be recognised! {playerReader.Target},{playerReader.PlayerBitValues.TargetIsDead},{playerReader.TargetHealth}");
                }
                else
                {
                    Log("Target Aquired");
                    SendActionEvent(new ActionEventArgs(GoapKey.newtarget, true));
                    break;
                }
            }
        }

        public override bool CheckIfActionCanRun()
        {
            if (this.playerReader.PlayerLevel == 60 && bagReader.BagsFull)
            {
                SendActionEvent(new ActionEventArgs(GoapKey.abort, true));
                wowProcess?.Hearthstone();
            }

            return !bagReader.BagsFull;
        }

        public async Task TapInteractKey(string source)
        {
            logger.LogInformation($"Approach target ({source})");
            await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey, 99);
            this.classConfiguration.Interact.SetClicked();
        }

        public async Task TapTargetLastTargetKey(string source)
        {
            logger.LogInformation($"Target Last Target ({source})");
            await this.wowProcess.KeyPress(this.classConfiguration.TargetLastTarget.ConsoleKey, 99);
            this.classConfiguration.TargetLastTarget.SetClicked();
        }

        private bool IsPlayerMoving(WowPoint lastPos)
        {
            var distance = WowPoint.DistanceTo(lastPos, playerReader.PlayerLocation);
            return distance > 0.5f;
        }

        private async Task WaitTilDropCombat(int maxNSequence)
        {
            int n = 0;
            while (playerReader.PlayerBitValues.PlayerInCombat && n < maxNSequence)
            {
                await playerReader.WaitForNUpdate(1);
                n++;
            }
            logger.LogInformation($"Dropped combat after {n}th update");
        }
    }
}