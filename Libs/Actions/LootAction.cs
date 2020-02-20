using Libs.GOAP;
using Libs.Looting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Libs.Actions
{
    public class LootAction : GoapAction
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly LootWheel lootWheel;
        private bool debug = true;

        public LootAction(WowProcess wowProcess, PlayerReader playerReader)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;
            lootWheel = new LootWheel(wowProcess, playerReader);
            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.hastarget, false);
        }

        private void Log(string text)
        {
            if (debug)
            {
                Debug.WriteLine($"{this.GetType().Name}: {text}");
            }
        }

        public override float CostOfPerformingAction { get => 4f; }

        public override bool CheckIfActionCanRun()
        {
            return true;
        }

        public override bool IsActionDone()
        {
            return false;
        }

        public override bool NeedsToBeInRangeOfTargetToExecute()
        {
            throw new NotImplementedException();
        }

        public override async Task PerformAction()
        {
            Log("Start PerformAction");

            // force stop turning
            wowProcess.KeyUp(ConsoleKey.LeftArrow);
            await Task.Delay(1);
            wowProcess.KeyUp(ConsoleKey.RightArrow);
            await Task.Delay(1);
            wowProcess.KeyUp(ConsoleKey.UpArrow);

            var healthAtStartOfLooting = playerReader.HealthPercent;

            await Task.Delay(500);

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

                Log(searchForMobs?"Searching for mobs": $"Looting (attempt: {lootAttempt + 1}.");
                var foundSomething = await lootWheel.Loot(searchForMobs);

                if (foundSomething && lootWheel.Classification == Cursor.CursorClassification.Kill)
                {
                    Log("We are being attacked!");

                    for (int i = 0; i < 2000; i += 100)
                    {
                        Log("Waiting for target to be recognised!");
                        await Task.Delay(100);
                        if (string.IsNullOrEmpty(playerReader.Target) && playerReader.PlayerBitValues.TargetIsDead && playerReader.TargetHealth==0)
                        {
                            await Task.Delay(100);
                            Log($"Waiting for target to be recognised! {playerReader.Target},{playerReader.PlayerBitValues.TargetIsDead},{playerReader.TargetHealth}");
                        }
                        else
                        {
                            Log("Target Aquired");
                            break;
                        }
                    }

                    return;
                }

                if (!foundSomething && !searchForMobs)
                {
                    lootAttempt = 10;
                }
                else
                {
                    if (searchForMobs)
                    {
                        searchForMobs = false;
                    }
                    else
                    {
                        if (lootWheel.Classification == Cursor.CursorClassification.Kill)
                        {
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

            Log("End PerformAction");
        }

        public override void ResetBeforePlanning()
        {
        }
    }
}