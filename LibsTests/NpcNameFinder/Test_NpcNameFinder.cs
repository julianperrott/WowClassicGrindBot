using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Libs;
using Microsoft.Extensions.Logging;

namespace LibsTests
{
    public class Test_NpcNameFinder
    {
        private readonly ILogger logger;
        private readonly NpcNameFinder npcNameFinder;

        public Test_NpcNameFinder(ILogger logger)
        {
            this.logger = logger;

            IRectProvider rectProvider = new RectProvider();
            MockWoWProcess mockWoWProcess = new MockWoWProcess();
            npcNameFinder = new NpcNameFinder(logger, rectProvider, mockWoWProcess);
        }

        public void Execute()
        {
            npcNameFinder.ChangeNpcType(NpcNameFinder.NPCType.Friendly);

            var npcs = npcNameFinder.RefreshNpcPositions();
            var bitmap = npcNameFinder.Screenshot.Bitmap;

            using (var gr = Graphics.FromImage(bitmap))
            {
                Font drawFont = new Font("Arial", 10);
                SolidBrush drawBrush = new SolidBrush(Color.White);

                if (npcs.Count > 0)
                {
                    using (var whitePen = new Pen(Color.White, 1))
                    {
                        npcs.ForEach(n => gr.DrawRectangle(whitePen, new Rectangle(n.Min, new Size(n.Width, n.Height))));
                        npcs.ForEach(n => gr.DrawString(npcs.IndexOf(n).ToString(), drawFont, drawBrush, new PointF(n.Min.X - 20, n.Min.Y)));
                    }
                }
            }

            npcs.ForEach(x =>
            {
                logger.LogInformation($"{npcs.IndexOf(x),2} -> {{X={x.Min.X,4},Y={x.Min.Y,4}}} - ({x.Width},{x.Height})");
            });

            logger.LogInformation("\n");

            bitmap.Save("names.png");
        }
    }
}
