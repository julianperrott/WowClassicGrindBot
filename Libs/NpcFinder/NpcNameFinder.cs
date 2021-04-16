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
        public enum NPCType
        {
            Enemy,
            Friendly,
            Neutral,
            FriendlyOrNeutral,
            Corpse
        }

        private NPCType _NPCType = NPCType.Enemy;

        private const int tick = 150;
        private bool ColorMatch(Color pixel)
        {
            switch(_NPCType)
            {
                case NPCType.FriendlyOrNeutral:
                    return (pixel.R == 0 && pixel.G > 250 && pixel.B == 0) ||
                        (pixel.R > 250 && pixel.G > 250 && pixel.B == 0);
                case NPCType.Friendly:
                    return pixel.R == 0 && pixel.G > 250 && pixel.B == 0;
                case NPCType.Neutral:
                    return pixel.R > 250 && pixel.G > 250 && pixel.B == 0;
                case NPCType.Corpse:
                    return pixel.R == 128 && pixel.G == 128 && pixel.B == 128;
                case NPCType.Enemy:
                default:
                    return pixel.R > 240 && pixel.G <= 35 && pixel.B <= 35;
            }
        }

        private List<LineOfNpcName> npcNameLine { get; set; } = new List<LineOfNpcName>();
        private List<List<LineOfNpcName>> npcs { get; set; } = new List<List<LineOfNpcName>>();

        private readonly ILogger logger;
        private readonly IRectProvider rectProvider;
        private readonly IMouseInput mouseInput;
        private DirectBitmap screen;

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

        private List<NpcPosition> Npcs { get; set; } = new List<NpcPosition>();
        public int NpcCount => npcs.Count;

        public int Sequence { get; private set; } = 0;

        private DateTime lastNpcFind = DateTime.Now.AddMilliseconds(-tick);

        public NpcNameFinder(ILogger logger, IRectProvider rectProvider, IMouseInput mouseInput)
        {
            this.logger = logger;
            this.rectProvider = rectProvider;
            this.mouseInput = mouseInput;
            this.screen = new DirectBitmap();
        }

        public void ChangeNpcType(NPCType type)
        {
            if(_NPCType != type)
            {
                _NPCType = type;
                lastNpcFind = DateTime.Now.AddMilliseconds(-200);
            }
        }

        public async Task FindAndClickNpc(int threshold)
        {
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
                        mouseInput.SetCursorPosition(clickPostion);
                        await Task.Delay(100);
                        CursorClassifier.Classify(out var cls).Dispose();
                        if (cls == CursorClassification.Kill)
                        {
                            await AquireTargetAtCursor(clickPostion, npc);
                            return;
                        }
                        else if(cls == CursorClassification.Vendor)
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
                //logger.LogInformation($"{ this.GetType().Name}.FindAndClickNpc: No NPC found!");
            }
        }

        public async Task<bool> FindByCursorType(CursorClassification cursor)
        {
            var locations = new List<Point>
            {
                new Point(0, 0),
                new Point(0, -25),
                new Point(-5, 10),
                new Point(5, 35),
                new Point(-5, 75),
                new Point(0, 125),
                new Point(0, 160),
            };

            foreach (var npc in Npcs)
            {
                foreach (var location in locations)
                {
                    var clickPostion = Screenshot.ToScreenCoordinates(npc.ClickPoint.X + location.X, npc.ClickPoint.Y + location.Y);
                    mouseInput.SetCursorPosition(clickPostion);
                    await Task.Delay(100);
                    CursorClassifier.Classify(out var cls).Dispose();
                    if (cls == cursor)
                    {
                        await AquireTargetAtCursor(clickPostion, npc);
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task AquireTargetAtCursor(Point clickPostion, NpcPosition npc)
        {
            await mouseInput.RightClickMouse(clickPostion);
            logger.LogInformation($"{ this.GetType().Name}.FindAndClickNpc: NPC found! Height={npc.Height}, width={npc.Width}");
        }

        public List<NpcPosition> RefreshNpcPositions()
        {
            if ((DateTime.Now - lastNpcFind).TotalMilliseconds > tick) //150
            {
                UpdateScreenshot();

                PopulateLinesOfNpcNames();
                DetermineNpcs();

                Npcs = npcs.OrderByDescending(npc => npc.Count)
                    .Select(s => new NpcPosition(new Point(s.Min(x => x.XStart), s.Min(x => x.Y)), new Point(s.Max(x => x.XEnd), s.Max(x => x.Y)), Screenshot.Width))
                    .Where(s => s.Width < 250) // 150 - fine // 200 - fine // 250 fine
                    .ToList();

                lastNpcFind = DateTime.Now;
                Sequence++;
            }

            UpdatePotentialAddsExist();
            return Npcs;
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
            if(!string.IsNullOrEmpty(info))
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
                        if (laterNpcLine.Y > lastY + 5) { break; } // 2

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

            const int minLength = 12; // original 22 // 18 fine
            const int lengthDiff = 3;
            const int minEndLength = minLength - lengthDiff; // original 18

            bool isEndOfSection;
            for (int y = 30; y < Screenshot.Height / 2; y++)
            {
                var lengthStart = -1;
                var lengthEnd = -1;
                for (int x = 0; x < Screenshot.Width; x++)
                {
                    var pixel = Screenshot.GetPixel(x, y);
                    var isTargetColor = ColorMatch(pixel);

                    if (isTargetColor)
                    {
                        var isSameSection = lengthStart > -1 && (x - lengthEnd) < minLength;

                        if (isSameSection)
                        {
                            lengthEnd = x;
                        }
                        else
                        {
                            isEndOfSection = lengthStart > -1 && lengthEnd - lengthStart > minEndLength;

                            if (isEndOfSection)
                            {
                                npcNameLine.Add(new LineOfNpcName(lengthStart, lengthEnd, y));
                            }

                            lengthStart = x;
                        }
                        lengthEnd = x;
                    }
                }

                isEndOfSection = lengthStart > -1 && lengthEnd - lengthStart > minEndLength;
                if (isEndOfSection)
                {
                    npcNameLine.Add(new LineOfNpcName(lengthStart, lengthEnd, y));
                }
            }
        }

        private void UpdateScreenshot()
        {
            rectProvider.GetWindowRect(out var rect);
            Screenshot = new DirectBitmap(rect);
            Screenshot.CaptureScreen();
        }

        public async Task WaitForNUpdate(int n)
        {
            var s = this.Sequence;
            while (this.Sequence <= s + n)
            {
                await Task.Delay(tick);
            }
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