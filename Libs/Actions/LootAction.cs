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


        public async Task<bool> AmIBeingTargetted()
        {
            // check for targets attacking me
            await wowProcess.KeyPress(ConsoleKey.Tab, 100);

            await Task.Delay(300);

            if (this.playerReader.HasTarget)
            {
                if (this.playerReader.PlayerBitValues.TargetOfTargetIsPlayer)
                {
                    await this.TapInteractKey("LootAction");
                    return true;
                }
                await wowProcess.KeyPress(ConsoleKey.F3, 200);
            }
            return false;
        }

        bool outOfCombat = false;

        public async Task<bool> CheckIfEnterredCombat()
        {
            if (!outOfCombat && !playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Left combat");
                outOfCombat = true;
            }

            if (outOfCombat && playerReader.PlayerBitValues.PlayerInCombat)
            {
                Log("Combat detected");
                return true;
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
            }

            return false;
        }

        public override async Task PerformAction()
        {
            await wowProcess.KeyPress(ConsoleKey.F9, 100); // stand
            await stopMoving.Stop();

            if (await AmIBeingTargetted()) { return; }

            var healthAtStartOfLooting = playerReader.HealthPercent;

            var outOfCombat = !playerReader.PlayerBitValues.PlayerInCombat;

            bool searchForMobs = playerReader.PlayerBitValues.PlayerInCombat;
            var lootAttempt = 0;
            while (lootAttempt < 10)
            {
                if (await CheckIfEnterredCombat()) { return; }
  
                Log(searchForMobs ? "Searching for mobs" : $"Looting (attempt: {lootAttempt + 1}.");
                var foundSomething = await lootWheel.Loot(searchForMobs, doExtendedLootSearch || foundAddWhileLooting);

                if (foundSomething && lootWheel.Classification == Cursor.CursorClassification.Kill)
                {
                    await AquireTarget();
                    return;
                }

                if (!foundSomething && !searchForMobs)
                {
                    lootAttempt = 10;
                    foundAddWhileLooting = false;
                    doExtendedLootSearch = false;
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

            RaiseEvent(new ActionEvent(GoapKey.shouldloot, false));
            RaiseEvent(new ActionEvent(GoapKey.postloot, true));
        }

        private async Task AquireTarget()
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
        }

        public override bool CheckIfActionCanRun()
        {
            if (this.playerReader.PlayerLevel==60 && bagReader.BagsFull)
            {
                RaiseEvent(new ActionEvent(GoapKey.abort, true));
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
    }
}