using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Extensions;
using SharedLib.NpcFinder;
using Game;
using Microsoft.Extensions.Logging;

namespace Core.Goals
{
    public class NpcNameTargeting
    {
        private const int MOUSE_DELAY = 40;

        private readonly ILogger logger;
        private readonly NpcNameFinder npcNameFinder;
        private readonly IMouseInput input;

        public int NpcCount => npcNameFinder.NpcCount;

        public List<Point> locTargetingAndClickNpc { get; private set; }
        public List<Point> locFindByCursorType { get; private set; }

        public NpcNameTargeting(ILogger logger, NpcNameFinder npcNameFinder, IMouseInput input)
        {
            this.logger = logger;
            this.npcNameFinder = npcNameFinder;
            this.input = input;


            locTargetingAndClickNpc = new List<Point>
            {
                new Point(0, 0),
                new Point(-10, 15).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
                new Point(10, 15).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
            };

            locFindByCursorType = new List<Point>
            {
                new Point(0, 0),
                new Point(0, 25).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
                new Point(0, 75).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
            };
        }

        public void ChangeNpcType(NpcNames npcNames)
        {
            npcNameFinder.ChangeNpcType(npcNames);
        }

        public async Task WaitForNUpdate(int n)
        {
            await npcNameFinder.WaitForNUpdate(n);
        }


        public async Task TargetingAndClickNpc(bool leftClick, CancellationToken cancellationToken)
        {
            if (npcNameFinder.NpcCount == 0)
                return;

            var npc = npcNameFinder.Npcs.First();
            logger.LogInformation($"> NPCs found: ({npc.Min.X},{npc.Min.Y})[{npc.Width},{npc.Height}]");

            foreach (var location in locTargetingAndClickNpc)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var clickPostion = npcNameFinder.ToScreenCoordinates(npc.ClickPoint.X + location.X, npc.ClickPoint.Y + location.Y);
                input.SetCursorPosition(clickPostion);
                await Task.Delay(MOUSE_DELAY);

                if (cancellationToken.IsCancellationRequested)
                    return;

                CursorClassifier.Classify(out var cls);
                if (cls == CursorType.Kill || cls == CursorType.Vendor)
                {
                    await AquireTargetAtCursor(clickPostion, npc, leftClick);
                    return;
                }
            }
        }

        public async Task<bool> FindBy(params CursorType[] cursor)
        {
            List<Point> attemptPoints = new List<Point>();

            foreach (var npc in npcNameFinder.Npcs)
            {
                attemptPoints.AddRange(locFindByCursorType);
                foreach(var point in locFindByCursorType)
                {
                    attemptPoints.Add(new Point(npc.Width / 2, point.Y).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight));
                    attemptPoints.Add(new Point(-npc.Width / 2, point.Y).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight));
                }

                foreach (var location in attemptPoints)
                {
                    var clickPostion = npcNameFinder.ToScreenCoordinates(npc.ClickPoint.X + location.X, npc.ClickPoint.Y + location.Y);
                    input.SetCursorPosition(clickPostion);
                    await Task.Delay(MOUSE_DELAY);
                    CursorClassifier.Classify(out var cls);
                    if (cursor.Contains(cls))
                    {
                        await AquireTargetAtCursor(clickPostion, npc);
                        return true;
                    }
                }
                attemptPoints.Clear();
            }
            return false;
        }

        private async Task AquireTargetAtCursor(Point clickPostion, NpcPosition npc, bool leftClick = false)
        {
            if (leftClick)
                await input.LeftClickMouse(clickPostion);
            else
                await input.RightClickMouse(clickPostion);

            logger.LogInformation($"{ this.GetType().Name}.FindAndClickNpc: NPC found! Height={npc.Height}, width={npc.Width}, pos={clickPostion}");
        }

        public void ShowClickPositions(Graphics gr)
        {
            if (NpcCount <= 0)
            {
                return;
            }

            using (var whitePen = new Pen(Color.White, 3))
            {
                npcNameFinder.Npcs.ForEach(n =>
                {
                    locFindByCursorType.ForEach(l =>
                    {
                        gr.DrawEllipse(whitePen, l.X + n.ClickPoint.X, l.Y + n.ClickPoint.Y, 5, 5);
                    });
                });
            }
        }

    }
}
