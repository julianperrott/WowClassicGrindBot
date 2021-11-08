using Microsoft.Extensions.Logging;
using SharedLib.Extensions;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Core
{
    public class CombatUtil
    {
        private readonly ILogger logger;
        private readonly PlayerReader playerReader;
        private readonly ConfigurableInput input;
        private readonly Wait wait;

        private readonly bool debug = true;

        private bool outOfCombat;
        private Vector3 lastPosition;

        public CombatUtil(ILogger logger, ConfigurableInput input, Wait wait, PlayerReader playerReader)
        {
            this.logger = logger;
            this.input = input;
            this.wait = wait;
            this.playerReader = playerReader;

            outOfCombat = !playerReader.Bits.PlayerInCombat;
            lastPosition = playerReader.PlayerLocation;
        }

        public void Update()
        {
            // TODO: have to find a better way to reset outOfCombat
            outOfCombat = !playerReader.Bits.PlayerInCombat;
            lastPosition = playerReader.PlayerLocation;
        }

        public async Task<bool> EnteredCombat()
        {
            await wait.Update(1);
            if (!outOfCombat && !playerReader.Bits.PlayerInCombat)
            {
                Log("Combat Leave");
                outOfCombat = true;
                return false;
            }

            if (outOfCombat && playerReader.Bits.PlayerInCombat)
            {
                Log("Combat Enter");
                outOfCombat = false;
                return true;
            }

            return false;
        }

        public async Task<bool> AquiredTarget()
        {
            if (this.playerReader.Bits.PlayerInCombat)
            {
                if (this.playerReader.PetHasTarget)
                {
                    await input.TapTargetPet();
                    Log($"Pets target {this.playerReader.TargetTarget}");
                    if (this.playerReader.TargetTarget == TargetTargetEnum.PetHasATarget)
                    {
                        await input.TapTargetOfTarget($"{GetType().Name}.AquiredTarget: Found target by pet");
                        return true;
                    }
                }

                await input.TapNearestTarget();
                await wait.Update(1);
                if (this.playerReader.HasTarget && playerReader.Bits.TargetInCombat &&
                    playerReader.Bits.TargetOfTargetIsPlayer)
                {
                    Log("Found from nearest target");
                    return true;
                }

                if (await Wait(200, () => playerReader.HasTarget))
                {
                    return true;
                }

                await input.TapClearTarget($"{GetType().Name}.AquiredTarget: No target found");
                await wait.Update(1);
            }
            return false;
        }

        public bool IsPlayerMoving(Vector3 lastPos)
        {
            var distance = playerReader.PlayerLocation.DistanceTo(lastPos);
            return distance > 0.01f;
        }

        public async Task<Tuple<bool, bool>> FoundTargetWhileMoved()
        {
            bool hadToMove = false;
            var startedMoving = await wait.InterruptTask(200, () => lastPosition != playerReader.PlayerLocation);
            if (!startedMoving.Item1)
            {
                Log($"  Goto corpse({startedMoving.Item2}ms) - Wait till player become stil!");
                hadToMove = true;
            }

            while (IsPlayerMoving(lastPosition))
            {
                lastPosition = playerReader.PlayerLocation;
                if (!await Wait(100, EnteredCombat()))
                {
                    if (await AquiredTarget())
                        return Tuple.Create(true, hadToMove);
                }
            }


            return Tuple.Create(false, hadToMove);
        }


        public static async Task<bool> Wait(int durationMs, Func<bool> exit)
        {
            int elapsedMs = 0;
            while (elapsedMs <= durationMs)
            {
                if (exit())
                    return false;

                await Task.Delay(50);
                elapsedMs += 50;
            }

            return true;
        }

        public static async Task<bool> Wait(int durationMs, Task<bool> exit)
        {
            int elapsedMs = 0;
            while (elapsedMs <= durationMs)
            {
                if (await exit)
                    return false;

                await Task.Delay(50);
                elapsedMs += 50;
            }

            return true;
        }


        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{GetType().Name}: {text}");
            }
        }
    }
}
