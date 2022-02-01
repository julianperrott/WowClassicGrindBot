using SharedLib.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Drawing.Imaging;
using System.Threading;

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

        private readonly List<LineOfNpcName> npcNameLine = new List<LineOfNpcName>();
        private readonly List<List<LineOfNpcName>> npcs = new List<List<LineOfNpcName>>();

        private readonly ILogger logger;
        private readonly IBitmapProvider bitmapProvider;
        private readonly AutoResetEvent autoResetEvent;

        public Rectangle Area { private set; get; }

        private const float refWidth = 1920;
        private const float refHeight = 1080;

        public float scaleToRefWidth { private set; get; } = 1;
        public float scaleToRefHeight { private set; get; } = 1;

        public List<NpcPosition> Npcs { get; private set; } = new List<NpcPosition>();
        public int NpcCount => Npcs.Count;
        public int AddCount { private set; get; }
        public int TargetCount { private set; get; }
        public bool MobsVisible => npcs.Count > 0;
        public bool PotentialAddsExist { get; private set; }
        public DateTime LastPotentialAddsSeen { get; private set; }

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


        public NpcNameFinder(ILogger logger, IBitmapProvider bitmapProvider, AutoResetEvent autoResetEvent)
        {
            this.logger = logger;
            this.bitmapProvider = bitmapProvider;
            this.autoResetEvent = autoResetEvent;
        }

        private float ScaleWidth(int value)
        {
            return value * (bitmapProvider.Bitmap.Width / refWidth);
        }

        private float ScaleHeight(int value)
        {
            return value * (bitmapProvider.Bitmap.Height / refHeight);
        }

        public void ChangeNpcType(NpcNames type)
        {
            if (nameType != type)
            {
                npcNameLine.Clear();
                npcs.Clear();
                Npcs.Clear();

                nameType = type;

                if (nameType.HasFlag(NpcNames.Corpse))
                {
                    npcPosYHeightMul = 15;
                }
                else
                {
                    npcPosYHeightMul = 10;
                }

                logger.LogInformation($"{nameof(NpcNameFinder)}.{nameof(ChangeNpcType)} = {type}");
            }
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
            scaleToRefWidth = ScaleWidth(1);
            scaleToRefHeight = ScaleHeight(1);

            Area = new Rectangle(new Point(0, (int)ScaleHeight(topOffset)),
                new Size(bitmapProvider.Bitmap.Width, (int)(bitmapProvider.Bitmap.Height * 0.6f)));

            PopulateLinesOfNpcNames();

            DetermineNpcs();

            Npcs = npcs.
                Select(s => new NpcPosition(new Point(s.Min(x => x.XStart), s.Min(x => x.Y)),
                    new Point(s.Max(x => x.XEnd), s.Max(x => x.Y)), bitmapProvider.Bitmap.Width, ScaleHeight(npcPosYOffset), ScaleHeight(npcPosYHeightMul)))
                .Where(s => s.Width < ScaleWidth(npcNameMaxWidth))
                .Distinct(new OverlappingNames())
                .OrderBy(npc => RectangleExt.SqrDistance(Area.BottomCentre(), npc.ClickPoint))
                .ToList();

            UpdatePotentialAddsExist();

            autoResetEvent.Set();
        }

        public void FakeUpdate()
        {
            npcNameLine.Clear();
            npcs.Clear();
            Npcs.Clear();

            autoResetEvent.Set();
        }

        public void UpdatePotentialAddsExist()
        {
            TargetCount = Npcs.Where(c => !c.IsAdd && Math.Abs(c.ClickPoint.X - c.screenMid) < c.screenTargetBuffer).Count();
            AddCount = Npcs.Where(c => c.IsAdd).Count();

            if (AddCount > 0 && TargetCount >= 1)
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
            npcs.Clear();
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
                            this.npcNameLine[j] = laterNpcLine;
                        }
                    }
                    if (group.Count > 0) { npcs.Add(group); }
                }
            }
        }

        private void PopulateLinesOfNpcNames()
        {
            npcNameLine.Clear();

            float minLength = ScaleWidth(LinesOfNpcMinLength);
            float lengthDiff = ScaleWidth(LinesOfNpcLengthDiff);
            float minEndLength = minLength - lengthDiff;

            unsafe
            {
                BitmapData bitmapData = bitmapProvider.Bitmap.LockBits(new Rectangle(0, 0, bitmapProvider.Bitmap.Width, bitmapProvider.Bitmap.Height), ImageLockMode.ReadOnly, bitmapProvider.Bitmap.PixelFormat);
                int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmapProvider.Bitmap.PixelFormat) / 8;

                for (int y = Area.Top; y < Area.Height; y += incY)
                {
                    bool isEndOfSection;
                    var lengthStart = -1;
                    var lengthEnd = -1;

                    byte* currentLine = (byte*)bitmapData.Scan0 + (y * bitmapData.Stride);
                    for (int x = Area.Left; x < Area.Right; x += incX)
                    {
                        int xi = x * bytesPerPixel;

                        if (ColorMatch(Color.FromArgb(255, currentLine[xi + 2], currentLine[xi + 1], currentLine[xi])))
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

                bitmapProvider.Bitmap.UnlockBits(bitmapData);
            }
        }

        public void WaitForNUpdate(int n)
        {
            while (n >= 0)
            {
                autoResetEvent.WaitOne();
                n--;
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
            return new Point(bitmapProvider.Rect.X + x, bitmapProvider.Rect.Top + y);
        }
    }
}