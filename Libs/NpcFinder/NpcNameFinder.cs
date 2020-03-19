using Libs.Actions;
using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.NpcFinder
{
    public class NpcNameFinder
    {
        public List<LineOfNpcName> npcNameLine { get; set; } = new List<LineOfNpcName>();
        public List<List<LineOfNpcName>> npcs { get; set; } = new List<List<LineOfNpcName>>();

        private ILogger logger;
        private readonly WowProcess wowProcess;
        private bool canFindNpcs = true;
        private DateTime pausedUntil = DateTime.Now;

        public NpcNameFinder(WowProcess wowProcess, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.logger = logger;
        }

        public async Task FindAndClickNpc(int threshold)
        {
            if (!canFindNpcs) { return; }

            var rect = wowProcess.GetWindowRect();
            using (DirectBitmap screenshot = new DirectBitmap(rect.right, rect.bottom))
            {
                screenshot.CaptureScreen();

                var npc = GetClosestNpc(screenshot);

                if (npc != null)
                {
                    var firstLine = npc.First();
                    if (npc.Count >= threshold)
                    {
                        if (DateTime.Now > pausedUntil)
                        {
                            await this.wowProcess.LeftClickMouse(screenshot.ToScreenCoordinates(firstLine.X, firstLine.Y + 35));
                            logger.LogInformation($"{ this.GetType().Name}: NPC found! Height={npc.Count}, width={firstLine.Length}");
                            await Task.Delay(300);
                        }
                        else
                        {
                            logger.LogInformation($"Paused until {pausedUntil.ToLongTimeString()}- { this.GetType().Name}: NPC found! Height={npc.Count}, width={firstLine.Length}");
                        }
                    }
                    else
                    {
                        logger.LogInformation($"{ this.GetType().Name}: NPC found but below threshold {threshold}! Height={npc.Count}, width={firstLine.Length}");
                    }
                }
                else
                {
                    logger.LogInformation($"{ this.GetType().Name}: NO NPC found!");
                }
            }
        }

        internal void StopFindingNpcs(int seconds)
        {
           pausedUntil = DateTime.Now.AddSeconds(seconds);
            logger.LogInformation($"Pause set until {pausedUntil.ToLongTimeString()}");
        }

        public int CountNpc(int threshold = 3)
        {
            var rect = wowProcess.GetWindowRect();
            using (var screenshot = new DirectBitmap(rect.right, rect.bottom))
            {
                screenshot.CaptureScreen();
                var npc = GetClosestNpc(screenshot);

                var count = npcs.Where(c => c.Count > threshold).Count();
                logger.LogInformation($"> NPCs count: {count}");

                return count;
            }
        }

        public List<LineOfNpcName>? GetClosestNpc(DirectBitmap directImage)
        {
            PopulateLinesOfNpcNames(directImage);
            DetermineNpcs();

            if (!npcs.Any())
            {
                return null;
            }

            var npcsInOrder = npcs.OrderByDescending(npc => npc.Count);

            var info = string.Join(", ", npcsInOrder.Select(n => n.Count.ToString() + $"({n.First().X},{n.First().Y})"));
            logger.LogInformation($"> NPCs found: {info}");

            return npcsInOrder.First();
        }

        private void DetermineNpcs()
        {
            npcs = new List<List<LineOfNpcName>>();
            for (int i = 0; i < npcNameLine.Count; i++)
            {
                var npcLine = this.npcNameLine[i];
                var group = new List<LineOfNpcName>() { npcLine };
                var lastY = npcLine.Y;

                if (!npcLine.IsInAgroup)
                {
                    for (int j = i + 1; j < this.npcNameLine.Count; j++)
                    {
                        var laterNpcLine = this.npcNameLine[j];
                        if (laterNpcLine.Y > npcLine.Y + 10) { break; }
                        if (laterNpcLine.Y > lastY + 2) { break; }

                        if (laterNpcLine.XStart <= npcLine.X && laterNpcLine.XEnd >= npcLine.X && laterNpcLine.Y > lastY)
                        {
                            laterNpcLine.IsInAgroup = true;
                            group.Add(laterNpcLine);
                            lastY = laterNpcLine.Y;
                        }
                    }
                    if (group.Count > 1) { npcs.Add(group); }
                }
            }
        }

        private void PopulateLinesOfNpcNames(DirectBitmap directImage)
        {
            npcNameLine = new List<LineOfNpcName>();

            bool isEndOfSection;
            for (int y = 0; y < directImage.Height; y++)
            {
                var lengthStart = -1;
                var lengthEnd = -1;
                for (int x = 0; x < directImage.Width; x++)
                {
                    var pixel = directImage.GetPixel(x, y);
                    var isRedPixel = pixel.R > 240 && pixel.G <= 35 && pixel.B <= 35;

                    if (isRedPixel)
                    {
                        var isSameSection = lengthStart > -1 && (x - lengthEnd) < 22;

                        if (isSameSection)
                        {
                            lengthEnd = x;
                        }
                        else
                        {
                            isEndOfSection = lengthStart > -1 && lengthEnd - lengthStart > 18;

                            if (isEndOfSection)
                            {
                                npcNameLine.Add(new LineOfNpcName(lengthStart, lengthEnd, y));
                            }

                            lengthStart = x;
                        }
                        lengthEnd = x;
                    }
                }

                isEndOfSection = lengthStart > -1 && lengthEnd - lengthStart > 18;
                if (isEndOfSection)
                {
                    npcNameLine.Add(new LineOfNpcName(lengthStart, lengthEnd, y));
                }
            }
        }

        public void OnActionEvent(object sender, ActionEvent e)
        {
            if (e.Key == GoapKey.fighting)
            {
                var newValue = (bool)e.Value == false;

                if (newValue != this.canFindNpcs)
                {
                    this.canFindNpcs = (bool)e.Value == false;
                    logger.LogInformation($"{this.GetType().Name}: Can find NPC = {this.canFindNpcs}");
                }
            }
        }
    }
}