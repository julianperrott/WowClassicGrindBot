using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading.Tasks;
using Core.Extensions;
using Game;

namespace Core.Looting
{
    public class LootWheel
    {
        private ILogger logger;

        private readonly WowScreen wowScreen;
        private readonly WowProcessInput input;
        private readonly PlayerReader playerReader;
        private readonly float num_theta = 32;
        private readonly float radiusLarge;
        private readonly float dtheta;
        private readonly Point centre;
        private readonly bool debug = true;
        

        public CursorType Classification { get; set; }

        private Point lastLootFoundAt;

        public LootWheel(ILogger logger, WowProcessInput input, WowScreen wowScreen, PlayerReader playerReader)
        {
            this.logger = logger;
            this.input = input;
            this.wowScreen = wowScreen;
            this.playerReader = playerReader;

            wowScreen.GetRectangle(out var rect);

            centre = new Point(rect.Centre().X, (int)((rect.Bottom / 5) * 3f));
            radiusLarge = rect.Bottom / 6;
            dtheta = (float)(2 * Math.PI / num_theta);
        }

        private void Log(string text)
        {
            if (debug && !string.IsNullOrEmpty(text))
            {
                //logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }

        public void Reset()
        {
            lastLootFoundAt = new Point(0, 0);
        }

        public async Task<bool> Loot(bool searchForMobs)
        {
            input.SetCursorPosition(new Point(this.lastLootFoundAt.X + 200, this.lastLootFoundAt.Y + 120));
            await Task.Delay(150);

            //if (!searchForMobs)
            //{
            //    WowProcess.SetCursorPosition(this.lastLootFoundAt);
            //    await Task.Delay(200);
            //}

            if (await CheckForLoot(this.lastLootFoundAt, searchForMobs, false))
            {
                logger.LogInformation($"Loot at {this.lastLootFoundAt.X},{this.lastLootFoundAt.Y}");
                return true;
            }
            else
            {
                logger.LogInformation($"No loot at {this.lastLootFoundAt.X},{this.lastLootFoundAt.Y}");
            }

            if (!searchForMobs)
            {
                if (await SearchInCircle(radiusLarge / 2, radiusLarge / 2, false, centre, false))
                {
                    return true;
                }
            }

            if (await SearchInCircle(radiusLarge, radiusLarge, searchForMobs, centre, false))
            {
                return true;
            }

            if (searchForMobs && lastLootFoundAt.X!=0)
            {
                await CheckForLoot(lastLootFoundAt, false, true);
            }

            return false;
        }

        private async Task<bool> SearchInCircle(float rx, float ry, bool searchForMobs, Point circleCentre, bool ignoreMobs)
        {
            float theta = 0;
            for (int i = 0; i < num_theta; i++)
            {
                float x = (float)(circleCentre.X + rx * Math.Cos(theta));
                float y = (float)(circleCentre.Y + (ry * Math.Sin(theta)));
                var mousePosition = new Point((int)x, (int)y);

                input.SetCursorPosition(mousePosition);

                if (await CheckForLoot(mousePosition, searchForMobs, ignoreMobs))
                {
                    return true;
                }

                theta += dtheta;
            }

            return false;
        }

        private async Task<bool> CheckForLoot(Point mousePosition, bool searchForMobs, bool ignoreMobs)
        {
            var inCombat = this.playerReader.Bits.PlayerInCombat;

            Classification = CursorType.None;
            await Task.Delay(30);

            CursorClassifier.Classify(out var cls);

            if (searchForMobs)
            {
                if (cls == CursorType.Kill)
                {
                    await Task.Delay(100);
                    CursorClassifier.Classify(out cls);
                }
            }
            else
            {
                // found something, lets give the cursor a chance to update.
                if (cls == CursorType.Loot || cls == CursorType.Kill || cls == CursorType.Skin)
                {
                    await Task.Delay(200);
                    CursorClassifier.Classify(out cls);
                }
            }

            if (cls == CursorType.Loot && !searchForMobs)
            {
                Log("Found: " + cls.ToString());
                await input.RightClickMouse(mousePosition);
                Classification = cls;
                await Task.Delay(500);
                await Wait(2000, inCombat);
            }

            if (cls == CursorType.Skin && !searchForMobs)
            {
                Log("Found: " + cls.ToString());
                await input.RightClickMouse(mousePosition);
                Classification = cls;
                await Task.Delay(1000);
                await Wait(6000, inCombat);
            }

            if (cls == CursorType.Kill && !ignoreMobs)
            {
                Log("Found: " + cls.ToString());
                await input.RightClickMouse(mousePosition);
                Classification = cls;
            }

            if (cls == CursorType.Loot || cls == CursorType.Skin)
            {
                lastLootFoundAt = mousePosition;
                logger.LogInformation($"Loot found at {this.lastLootFoundAt.X},{this.lastLootFoundAt.Y}");
            }

            if (searchForMobs)
            {
                return cls == CursorType.Kill;
            }

            return cls == CursorType.Loot || cls == CursorType.Skin || cls == CursorType.Kill;
        }

        private async Task Wait(int delay, bool isInCombat)
        {
            for (int i = 0; i < delay; i += 100)
            {
                if (!isInCombat && this.playerReader.Bits.PlayerInCombat)
                {
                    logger.LogInformation("We have enterred combat, aborting loot");
                    return;
                }
                if (!this.playerReader.IsCasting)
                {
                    CursorClassifier.Classify(out var cls2);
                    if (cls2 != this.Classification)
                    {
                        return;
                    }
                }
                await Task.Delay(100);
            }
            return;
        }
    }
}