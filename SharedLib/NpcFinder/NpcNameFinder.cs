using SharedLib.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace SharedLib.NpcFinder
{
    [Flags]
    public enum NpcNames
    {
        None = 0,
        Enemy = 1,
        Friendly = 2,
        Neutral = 4,
        Corpse = 8
    }

    public class NpcNameFinder
    {
        private NpcNames nameType = NpcNames.Enemy | NpcNames.Neutral;

        private List<LineOfNpcName> npcNameLine { get; set; } = new List<LineOfNpcName>();
        private List<List<LineOfNpcName>> npcs { get; set; } = new List<List<LineOfNpcName>>();

        private readonly ILogger logger;
        private readonly IDirectBitmapProvider bitmapProvider;

        public Rectangle Area { private set; get; }

        private const float refWidth = 1920;
        private const float refHeight = 1080;

        public float scaleToRefWidth { private set; get; } = 1;
        public float scaleToRefHeight { private set; get; } = 1;

        public List<NpcPosition> Npcs { get; private set; } = new List<NpcPosition>();
        public int NpcCount => npcs.Count;
        public int AddCount { private set; get; }
        public int TargetCount { private set; get; }
        public bool MobsVisible => npcs.Count > 0;
        public bool PotentialAddsExist { get; private set; }

        private bool Enabled = true;

        public DateTime LastPotentialAddsSeen { get; private set; } = default;

        public int Sequence { get; private set; } = 0;


        #region variables

        public int topOffset { get; set; } = 30;

        public int npcPosYOffset { get; set; } = 0;
        public int npcPosYHeightMul { get; set; } = 10;

        public int npcNameMaxWidth { get; set; } = 250;

        public int LinesOfNpcMinLength { get; set; } = 22;

        public int LinesOfNpcLengthDiff { get; set; } = 4;

        public int DetermineNpcsHeightOffset1 { get; set; } = 10;

        public int DetermineNpcsHeightOffset2 { get; set; } = 2;

        public int incX { get; set; } = 1;

        public int incY { get; set; } = 1;

        #endregion


        public NpcNameFinder(ILogger logger, IDirectBitmapProvider bitmapProvider)
        {
            this.logger = logger;
            this.bitmapProvider = bitmapProvider;
        }

        private float ScaleWidth(int value)
        {
            return value * (bitmapProvider.DirectBitmap.Width / refWidth);
        }

        private float ScaleHeight(int value)
        {
            return value * (bitmapProvider.DirectBitmap.Height / refHeight);
        }

        public void ChangeNpcType(NpcNames type)
        {
            if (nameType != type)
            {
                nameType = type;

                if (nameType.HasFlag(NpcNames.Corpse))
                {
                    npcPosYHeightMul = 15;
                }
                else
                {
                    npcPosYHeightMul = 10;
                }

                logger.LogInformation($"{GetType().Name}.ChangeNpcType = {type}");
            }
        }

        public void Disable()
        {
            Enabled = false;
        }

        public void Enable()
        {
            Enabled = true;
        }

        private bool ColorMatch(Color p)
        {
            return nameType switch
            {
                NpcNames.Enemy | NpcNames.Neutral => (p.R > 240 && p.G <= 35 && p.B <= 35) || (p.R > 250 && p.G > 250 && p.B == 0),
                NpcNames.Friendly | NpcNames.Neutral => (p.R == 0 && p.G > 250 && p.B == 0) || (p.R > 250 && p.G > 250 && p.B == 0),
                NpcNames.Enemy => p.R > 240 && p.G <= 35 && p.B <= 35,
                NpcNames.Friendly => p.R == 0 && p.G > 250 && p.B == 0,
                NpcNames.Neutral => p.R > 250 && p.G > 250 && p.B == 0,
                NpcNames.Corpse => p.R == 128 && p.G == 128 && p.B == 128,
                _ => false,
            };
        }


        public void Update()
        {
            if (Enabled)
            {

                scaleToRefWidth = ScaleWidth(1);
                scaleToRefHeight = ScaleHeight(1);

                Area = new Rectangle(new Point(0, (int)ScaleHeight(topOffset)),
                    new Size(bitmapProvider.DirectBitmap.Width, (int)(bitmapProvider.DirectBitmap.Height * 0.6f)));

                PopulateLinesOfNpcNames();

                DetermineNpcs();

                Npcs = npcs.
                    Select(s => new NpcPosition(new Point(s.Min(x => x.XStart), s.Min(x => x.Y)),
                        new Point(s.Max(x => x.XEnd), s.Max(x => x.Y)), bitmapProvider.DirectBitmap.Width, ScaleHeight(npcPosYOffset), ScaleHeight(npcPosYHeightMul)))
                    .Where(s => s.Width < ScaleWidth(npcNameMaxWidth))
                    .Distinct(new OverlappingNames())
                    .OrderBy(npc => RectangleExt.SqrDistance(Area.BottomCentre(), npc.ClickPoint))
                    .ToList();

                UpdatePotentialAddsExist();
            }
            else
            {
                npcs.Clear();
                Npcs.Clear();
            }

            Sequence++;
        }

        public void UpdatePotentialAddsExist()
        {
            TargetCount = Npcs.Where(c => !c.IsAdd && Math.Abs(c.ClickPoint.X - c.screenMid) < c.screenTargetBuffer).Count();
            AddCount = Npcs.Where(c => c.IsAdd).Count();

            if (AddCount > 0 || TargetCount > 1)
            {
                PotentialAddsExist = true;
                LastPotentialAddsSeen = DateTime.Now;
            }
            else
            {
                if (PotentialAddsExist && (DateTime.Now - LastPotentialAddsSeen).TotalSeconds > 1)
                {
                    PotentialAddsExist = false;
                    AddCount = 0;
                }
            }
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
                        if (laterNpcLine.Y > npcLine.Y + ScaleHeight(DetermineNpcsHeightOffset1)) { break; } // 10
                        if (laterNpcLine.Y > lastY + ScaleHeight(DetermineNpcsHeightOffset2)) { break; } // 5

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

            float minLength = ScaleWidth(LinesOfNpcMinLength);
            float lengthDiff = ScaleWidth(LinesOfNpcLengthDiff);
            float minEndLength = minLength - lengthDiff;

            bool isEndOfSection;
            for (int y = Area.Top; y < Area.Height; y += incY)
            {
                var lengthStart = -1;
                var lengthEnd = -1;
                for (int x = Area.Left; x < Area.Right; x += incX)
                {
                    if (ColorMatch(bitmapProvider.DirectBitmap.GetPixel(x, y)))
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

        public async ValueTask WaitForNUpdate(int n)
        {
            var s = this.Sequence;
            while (this.Sequence <= s + n)
            {
                await Task.Delay(10);
            }
        }

        public void ShowNames(Graphics gr)
        {
            if (Npcs.Count <= 0)
            {
                return;
            }

            using var whitePen = new Pen(Color.White, 3);
            using var greyPen = new Pen(Color.Gray, 3);

            /*
            if (Npcs.Any())
            {
                // target area
                gr.DrawLine(whitePen, new Point(Npcs[0].screenMid - Npcs[0].screenTargetBuffer, Area.Top), new Point(Npcs[0].screenMid - Npcs[0].screenTargetBuffer, Area.Bottom));
                gr.DrawLine(whitePen, new Point(Npcs[0].screenMid + Npcs[0].screenTargetBuffer, Area.Top), new Point(Npcs[0].screenMid + Npcs[0].screenTargetBuffer, Area.Bottom));

                // adds area
                gr.DrawLine(greyPen, new Point(Npcs[0].screenMid - Npcs[0].screenAddBuffer, Area.Top), new Point(Npcs[0].screenMid - Npcs[0].screenAddBuffer, Area.Bottom));
                gr.DrawLine(greyPen, new Point(Npcs[0].screenMid + Npcs[0].screenAddBuffer, Area.Top), new Point(Npcs[0].screenMid + Npcs[0].screenAddBuffer, Area.Bottom));
            }
            */

            Npcs.ForEach(n => gr.DrawRectangle(n.IsAdd ? greyPen : whitePen, new Rectangle(n.Min, new Size(n.Width, n.Height))));
        }

        public Point ToScreenCoordinates(int x, int y)
        {
            return bitmapProvider.DirectBitmap.ToScreenCoordinates(x, y);
        }
    }
}