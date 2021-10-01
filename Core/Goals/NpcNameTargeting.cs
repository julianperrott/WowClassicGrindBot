using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Cursor;
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
                new Point(-45, 50).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
                new Point(45, 50).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
                new Point(25, 90).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
                new Point(-25, 130).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
                new Point(0, 160).Scale(npcNameFinder.scaleToRefWidth, npcNameFinder.scaleToRefHeight),
            };

            locFindByCursorType.Reverse();
        }

        public void ChangeNpcType(NpcNames npcNames)
        {
            npcNameFinder.ChangeNpcType(npcNames);
        }

        public async Task WaitForNUpdate(int n)
        {
            await npcNameFinder.WaitForNUpdate(n);
        }


        public async Task TargetingAndClickNpc(int threshold, bool leftClick, CancellationToken cancellationToken)
        {
            var npc = npcNameFinder.GetClosestNpc();
            if (npc != null)
            {
                if (npc.Height >= threshold)
                {
                    foreach (var location in locTargetingAndClickNpc)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        var clickPostion = npcNameFinder.ToScreenCoordinates(npc.ClickPoint.X + location.X, npc.ClickPoint.Y + location.Y);
                        input.SetCursorPosition(clickPostion);
                        await Task.Delay(MOUSE_DELAY);
                        CursorClassifier.Classify(out var cls).Dispose();
                        if (cls == CursorClassification.Kill)
                        {
                            await AquireTargetAtCursor(clickPostion, npc, leftClick);
                            return;
                        }
                        else if (cls == CursorClassification.Vendor)
                        {
                            await AquireTargetAtCursor(clickPostion, npc, leftClick);
                            return;
                        }
                    }
                }
                else
                {
                    logger.LogInformation($"{this.GetType().Name}: NPC found but below threshold {threshold}! Height={npc.Height}, width={npc.Width}");
                }
            }
            else
            {
                //logger.LogInformation($"{ this.GetType().Name}.FindAndClickNpc: No NPC found!");
            }
        }

        public async Task<bool> FindByCursorType(params CursorClassification[] cursor)
        {
            foreach (var npc in npcNameFinder.Npcs)
            {
                foreach (var location in locFindByCursorType)
                {
                    var clickPostion = npcNameFinder.ToScreenCoordinates(npc.ClickPoint.X + location.X, npc.ClickPoint.Y + location.Y);
                    input.SetCursorPosition(clickPostion);
                    await Task.Delay(MOUSE_DELAY);
                    CursorClassifier.Classify(out var cls).Dispose();
                    if (cursor.Contains(cls))
                    {
                        await AquireTargetAtCursor(clickPostion, npc);
                        return true;
                    }
                }
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

    }
}
