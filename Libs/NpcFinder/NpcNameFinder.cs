using Libs.Goals;
using Libs.Cursor;
using Libs.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Libs
{
    public sealed class NpcNameFinder : IDisposable
    {
        private List<LineOfNpcName> npcNameLine { get; set; } = new List<LineOfNpcName>();
        private List<List<LineOfNpcName>> npcs { get; set; } = new List<List<LineOfNpcName>>();
        private readonly PlayerReader playerReader;

        private ILogger logger;
        private readonly WowProcess wowProcess;
        //private bool canFindNpcs = true;

        private List<NpcPosition> Npcs { get; set; } = new List<NpcPosition>();
        private DateTime lastNpcFind = DateTime.Now;

        private DirectBitmap screen = new DirectBitmap(1, 1);

        public DirectBitmap Screenshot
        {
            get
            {
                return screen;
            }
            set
            {
                if (screen != null)
                {
                    screen.Dispose();
                }
                screen = value;
            }
        }

        public NpcNameFinder(WowProcess wowProcess, PlayerReader playerReader, ILogger logger)
        {
            this.wowProcess = wowProcess;
            this.logger = logger;
            this.playerReader = playerReader;
        }

        public async Task FindAndClickNpc(int threshold)
        {
            //if (!canFindNpcs) { return; }

            if (this.playerReader.HasTarget)
            {
                return;
            }

            var npc = GetClosestNpc();

            if (npc != null)
            {
                if (npc.Height >= threshold)
                {
                    var locations = new List<Point>
                    {
                        new Point(0,0),
                        new Point(10,10),
                        new Point(-10,-10),
                        new Point(20,20),
                        new Point(-20,-20),
                    };

                    foreach (var location in locations)
                    {
                        var clickPostion = Screenshot.ToScreenCoordinates(npc.ClickPoint.X + location.X, npc.ClickPoint.Y + location.Y);

                        clickPostion.X = wowProcess.ScaleDown(clickPostion.X);
                        clickPostion.Y = wowProcess.ScaleDown(clickPostion.Y);

                        WowProcess.SetCursorPosition(clickPostion);
                        await Task.Delay(100);
                        CursorClassifier.Classify(out var cls).Dispose();
                        if (cls == CursorClassification.Kill)
                        {
                            await AquireTargetAtCursor(clickPostion, npc);
                            return;
                        }
                    }
                }
                else
                {
                    logger.LogInformation($"{ this.GetType().Name}.FindAndClickNpc: NPC found but below threshold {threshold}! Height={npc.Height}, width={npc.Width}");
                }
            }
            else
            {
                logger.LogInformation($"{ this.GetType().Name}.FindAndClickNpc: No NPC found!");
            }
        }

        private async Task AquireTargetAtCursor(Point clickPostion, NpcPosition npc)
        {
            await this.wowProcess.RightClickMouse(clickPostion);
            logger.LogInformation($"{ this.GetType().Name}.FindAndClickNpc: NPC found! Height={npc.Height}, width={npc.Width}");
            //for (int i = 0; i < 6; i++)
            //{
            //    if (this.playerReader.HasTarget)
            //    {
            //        break;
            //    }
            //    await this.wowProcess.KeyPress(ConsoleKey.Tab, 100);
            //    clickPostion = Screenshot.ToScreenCoordinates(npc.ClickPoint.X + 15 - random.Next(30), npc.ClickPoint.Y);
            //    wowProcess.SetCursorPosition(clickPostion);
            //}
        }

        public List<NpcPosition> RefreshNpcPositions()
        {
            UpdateScreenshot();

            if (!this.playerReader.PlayerBitValues.PlayerInCombat)
            {
                if ((DateTime.Now - lastNpcFind).TotalMilliseconds > 500)
                {
                    PopulateLinesOfNpcNames();
                    DetermineNpcs();

                    Npcs = npcs.OrderByDescending(npc => npc.Count)
                        .Select(s => new NpcPosition(new Point(s.Min(x => x.XStart), s.Min(x => x.Y)), new Point(s.Max(x => x.XEnd), s.Max(x => x.Y)), Screenshot.Width))
                        .Where(s => s.Width < 150)
                        .ToList();

                    lastNpcFind = DateTime.Now;
                }

                UpdatePotentialAddsExist();

                return Npcs;
            }

            return new List<NpcPosition>();
        }

        public bool MobsVisible { get; private set; }
        public bool PotentialAddsExist { get; private set; }
        public DateTime LastPotentialAddsSeen { get; private set; } = DateTime.Now.AddMinutes(-1);

        public void UpdatePotentialAddsExist()
        {
            var countAdds = Npcs.Where(c => c.IsAdd).Where(c => c.Height > 2).Count();
            var MobsVisible = Npcs.Where(c => c.Height > 2).Any();

            if (countAdds > 0)
            {
                PotentialAddsExist = true;
                LastPotentialAddsSeen = DateTime.Now;
            }
            else
            {
                if (PotentialAddsExist && (DateTime.Now - LastPotentialAddsSeen).TotalSeconds > 2)
                {
                    PotentialAddsExist = false;
                }
            }
        }

        public NpcPosition? GetClosestNpc()
        {
            var info = string.Join(", ", Npcs.Select(n => n.Height.ToString() + $"({n.Min.X},{n.Min.Y})"));
            logger.LogInformation($"> NPCs found: {info}");

            return Npcs.Count == 0 ? null : Npcs.First();
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
                    if (group.Count > 0) { npcs.Add(group); }
                }
            }
        }

        private void PopulateLinesOfNpcNames()
        {
            npcNameLine = new List<LineOfNpcName>();

            bool isEndOfSection;
            for (int y = 30; y < Screenshot.Height / 2; y++)
            {
                var lengthStart = -1;
                var lengthEnd = -1;
                for (int x = 0; x < Screenshot.Width; x++)
                {
                    var pixel = Screenshot.GetPixel(x, y);
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

        private void UpdateScreenshot()
        {
            var rect = wowProcess.GetWindowRect();
            Screenshot = new DirectBitmap(rect.right, rect.bottom, 0, 0);
            Screenshot.CaptureScreen();
        }

        public void Dispose()
        {
            if (screen != null)
            {
                screen.Dispose();
            }
        }
    }
}