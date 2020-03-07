using Libs.Cursor;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace Libs.Looting
{
    public class LootWheel
    {
        private readonly WowProcess wowProcess;
        private readonly PlayerReader playerReader;
        private readonly float num_theta = 32;
        private readonly float radiusLarge;
        private readonly float dtheta;
        private readonly Point centre;
        private readonly bool debug = true;

        public CursorClassification Classification { get; set; }

        private Point lastLootFoundAt;

        public LootWheel(WowProcess wowProcess, PlayerReader playerReader)
        {
            this.wowProcess = wowProcess;
            this.playerReader = playerReader;

            var rect = wowProcess.GetWindowRect();

            centre = new Point((int)(rect.right / 2f), (int)((rect.bottom / 5) * 3f));
            radiusLarge = rect.bottom / 6;
            dtheta = (float)(2 * Math.PI / num_theta);
        }

        private void Log(string text)
        {
            if (debug)
            {
                Debug.WriteLine($"{this.GetType().Name}: {text}");
            }
        }

        public async Task<bool> Loot(bool searchForMobs)
        {
            if (!searchForMobs)
            {
                wowProcess.SetCursorPosition(new Point(this.lastLootFoundAt.X + 200, this.lastLootFoundAt.Y + 120));
                await Task.Delay(150);
                wowProcess.SetCursorPosition(this.lastLootFoundAt);
                await Task.Delay(200);
            }
            if (await CheckForLoot(this.lastLootFoundAt, searchForMobs))
            {
                Debug.WriteLine($"Loot at {this.lastLootFoundAt.X},{this.lastLootFoundAt.Y}");
                return true;
            }
            else
            {
                Debug.WriteLine($"No loot at {this.lastLootFoundAt.X},{this.lastLootFoundAt.Y}");
            }

            if (!searchForMobs)
            {
                if (await SearchInCircle(radiusLarge / 2, radiusLarge / 2, false))
                {
                    return true;
                }
            }

            return await SearchInCircle(radiusLarge, radiusLarge, searchForMobs);
        }

        private async Task<bool> SearchInCircle(float rx, float ry, bool searchForMobs)
        {
            float theta = 0;
            for (int i = 0; i < num_theta; i++)
            {
                float x = (float)(centre.X + rx * Math.Cos(theta));
                float y = (float)(centre.Y + (ry * Math.Sin(theta)));
                var mousePosition = new Point((int)x, (int)y);

                wowProcess.SetCursorPosition(mousePosition);

                if (await CheckForLoot(mousePosition, searchForMobs))
                {
                    return true;
                }

                theta += dtheta;
            }

            return false;
        }

        private async Task<bool> CheckForLoot(Point mousePosition, bool searchForMobs)
        {
            var inCombat = this.playerReader.PlayerBitValues.PlayerInCombat;

            Classification = CursorClassification.None;
            await Task.Delay(30);

            CursorClassifier.Classify(out var cls);

            if (searchForMobs)
            {
                if (cls == CursorClassification.Kill)
                {
                    await Task.Delay(100);
                    CursorClassifier.Classify(out cls);
                }
            }
            else
            {
                // found something, lets give the cursor a chance to update.
                if (cls == CursorClassification.Loot || cls == CursorClassification.Kill || cls == CursorClassification.Skin)
                {
                    await Task.Delay(200);
                    CursorClassifier.Classify(out cls);
                }
            }

            if (cls == CursorClassification.Loot && !searchForMobs)
            {
                Log("Found: " + cls.ToString());
                await wowProcess.RightClickMouse(mousePosition);
                Classification = cls;
                await Task.Delay(500);
                await Wait(2000, inCombat);
            }

            if (cls == CursorClassification.Skin && !searchForMobs)
            {
                Log("Found: " + cls.ToString());
                await wowProcess.RightClickMouse(mousePosition);
                Classification = cls;
                await Task.Delay(1000);
                await Wait(6000, inCombat);
            }

            if (cls == CursorClassification.Kill)
            {
                Log("Found: " + cls.ToString());
                await wowProcess.RightClickMouse(mousePosition);
                Classification = cls;
            }

            if (cls == CursorClassification.Loot || cls == CursorClassification.Skin)
            {
                lastLootFoundAt = mousePosition;
                Debug.WriteLine($"Loot found at {this.lastLootFoundAt.X},{this.lastLootFoundAt.Y}");
            }

            if (searchForMobs)
            {
                return cls == CursorClassification.Kill;
            }

            return cls == CursorClassification.Loot || cls == CursorClassification.Skin || cls == CursorClassification.Kill;
        }

        private async Task Wait(int delay, bool isInCombat)
        {
            for (int i = 0; i < delay; i += 100)
            {
                if (!isInCombat & this.playerReader.PlayerBitValues.PlayerInCombat)
                {
                    Debug.WriteLine("We have enterred combat, aborting loot");
                    return;
                }

                CursorClassifier.Classify(out var cls2);
                if (cls2 != this.Classification)
                {
                    return;
                }
                await Task.Delay(100);
            }
            return;
        }
    }
}