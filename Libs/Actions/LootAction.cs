using Libs.GOAP;
using Libs.Looting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class LootAction : GoapAction
    {
        protected readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly LootWheel lootWheel;
        private readonly StopMoving stopMoving;
        private readonly BagReader bagReader;
        private readonly ClassConfiguration classConfiguration;
        private ILogger logger;

        private bool debug = true;

        public LootAction(WowProcess wowProcess, PlayerReader playerReader, BagReader bagReader, StopMoving stopMoving, ILogger logger, ClassConfiguration classConfiguration)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            this.stopMoving = stopMoving;
            this.bagReader = bagReader;
            this.logger = logger;
            this.classConfiguration = classConfiguration;

            lootWheel = new LootWheel(wowProcess, playerReader, logger);

            AddPreconditions();
        }

        protected virtual void AddPreconditions()
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

        private bool foundAddWhileLooting = false;
        private bool doExtendedLootSearch = true;

        public override async Task PerformAction()
        {
            await stopMoving.Stop();

            // check for targets attacking me
            await wowProcess.KeyPress(ConsoleKey.Tab, 100);
            
            await Task.Delay(300);

            if (this.playerReader.HasTarget)
            {
                if (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
                {
                    await this.TapInteractKey("LootAction");
                    return;
                }
                await wowProcess.KeyPress(ConsoleKey.F3, 200);
            }

            await wowProcess.KeyPress(ConsoleKey.F9, 100); // stand

            var healthAtStartOfLooting = playerReader.HealthPercent;

            //await Task.Delay(1000);

            bool outOfCombat = false;

            bool searchForMobs = true;
            var lootAttempt = 0;
            while (lootAttempt < 10)
            {
                if (!outOfCombat && !playerReader.PlayerBitValues.PlayerInCombat)
                {
                    Log("Left combat");
                    outOfCombat = true;
                }

                if (outOfCombat && playerReader.PlayerBitValues.PlayerInCombat)
                {
                    Log("Combat detected");
                    return;
                }

                Log(searchForMobs ? "Searching for mobs" : $"Looting (attempt: {lootAttempt + 1}.");
                var foundSomething = await lootWheel.Loot(searchForMobs, doExtendedLootSearch || foundAddWhileLooting);

                if (foundSomething && lootWheel.Classification == Cursor.CursorClassification.Kill)
                {
                    foundAddWhileLooting = true;
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
                            RaiseEvent(new ActionEvent(GoapKey.newtarget, true));
                            break;
                        }
                    }

                    return;
                }

                if (this.playerReader.PlayerBitValues.PlayerInCombat && this.playerReader.PlayerClass == PlayerClassEnum.Warlock)
                {
                    // /Target pet
                    await wowProcess.KeyPress(ConsoleKey.F12, 300);

                    if (this.playerReader.TargetTarget == TargetTargetEnum.PetHasATarget)
                    {
                        await wowProcess.KeyPress(ConsoleKey.U, 200); // switch to pet's target /tar targettarget
                        return;
                    }
                }

                if (!foundSomething && !searchForMobs)
                {
                    lootAttempt = 10;
                    foundAddWhileLooting = false;
                    doExtendedLootSearch = true;
                }
                else
                {
                    doExtendedLootSearch = false;
                    if (searchForMobs)
                    {
                        searchForMobs = false;
                    }
                    else
                    {
                        if (lootWheel.Classification == Cursor.CursorClassification.Kill)
                        {
                            foundAddWhileLooting = true;
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

            // wait until we have left combat
            // check for enemies
            // is our health going down.

            //await wowProcess.RightClickMouse(new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width / 2, (Screen.PrimaryScreen.Bounds.Height / 2)));

            RaiseEvent(new ActionEvent(GoapKey.shouldloot, false));
            RaiseEvent(new ActionEvent(GoapKey.postloot, true));

            if (bagReader.BagsFull)
            //if (bagReader.bagItems.Count > 52)
            {
                logger.LogInformation("bags full");
                //RaiseEvent(new ActionEvent(GoapKey.abort, true));
            }

            Log("End PerformAction");
        }

        public override bool CheckIfActionCanRun()
        {
            return !bagReader.BagsFull;
        }

        public async Task TapInteractKey(string source)
        {
            logger.LogInformation($"Approach target ({source})");
            await this.wowProcess.KeyPress(this.classConfiguration.Interact.ConsoleKey, 99);
            this.classConfiguration.Interact.SetClicked();
        }
    }
}