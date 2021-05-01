using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Core;
using Microsoft.Extensions.Logging;
using SharedLib;

namespace CoreTests
{
    public class Test_NpcNameFinder
    {
        private readonly ILogger logger;
        private readonly NpcNameFinder npcNameFinder;
        private readonly RectProvider rectProvider;
        private readonly DirectBitmapCapturer capturer;

        public Test_NpcNameFinder(ILogger logger)
        {
            this.logger = logger;

            MockWoWProcess mockWoWProcess = new MockWoWProcess();
            rectProvider = new RectProvider();
            rectProvider.GetRectangle(out var rect);
            capturer = new DirectBitmapCapturer(rect);

            npcNameFinder = new NpcNameFinder(logger, capturer, mockWoWProcess);
        }

        public void Execute()
        {
            npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.FriendlyOrNeutral);

            capturer.Capture();
            this.npcNameFinder.Update();

            var bitmap = capturer.GetBitmap(capturer.Rect.Width, capturer.Rect.Height);

            using (var gr = Graphics.FromImage(bitmap))
            {
                Font drawFont = new Font("Arial", 10);
                SolidBrush drawBrush = new SolidBrush(Color.White);

                if (npcNameFinder.Npcs.Count > 0)
                {
                    using (var whitePen = new Pen(Color.White, 1))
                    {
                        npcNameFinder.Npcs.ForEach(n => gr.DrawRectangle(whitePen, new Rectangle(n.Min, new Size(n.Width, n.Height))));
                        npcNameFinder.Npcs.ForEach(n => gr.DrawString(npcNameFinder.Npcs.IndexOf(n).ToString(), drawFont, drawBrush, new PointF(n.Min.X - 20, n.Min.Y)));
                    }
                }
            }

            npcNameFinder.Npcs.ForEach(x =>
            {
                logger.LogInformation($"{npcNameFinder.Npcs.IndexOf(x),2} -> {{X={x.Min.X,4},Y={x.Min.Y,4}}} - ({x.Width},{x.Height})");
            });

            logger.LogInformation("\n");

            bitmap.Save("names.png");
        }
    }
}
