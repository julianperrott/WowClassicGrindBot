using Libs.Actions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Libs
{
    public class ActionFactory
    {
        private readonly AddonReader addonReader;
        public NpcNameFinder NpcNameFinder { get; private set; }
        private readonly WowProcess wowProcess;
        private ILogger logger;

        public bool PotentialAddsExist => NpcNameFinder.PotentialAddsExist;

        public RouteInfo? RouteInfo { get; private set; }

        public ActionFactory(AddonReader addonReader, ILogger logger, WowProcess wowProcess, NpcNameFinder npcNameFinder)
        {
            this.logger = logger;
            this.addonReader = addonReader;
            this.NpcNameFinder = npcNameFinder;
            this.wowProcess = wowProcess;
        }

        public HashSet<GoapAction> CreateActions(ClassConfiguration classConfig, IBlacklist blacklist)
        {
            var availableActions = new HashSet<GoapAction>();

            List<WowPoint> pathPoints, spiritPath;
            GetPaths(out pathPoints, out spiritPath, classConfig);

            var playerDirection = new PlayerDirection(addonReader.PlayerReader, wowProcess, logger);
            var stopMoving = new StopMoving(wowProcess, addonReader.PlayerReader);

            var castingHandler = new CastingHandler(wowProcess, addonReader.PlayerReader, logger, classConfig, playerDirection, NpcNameFinder);

            var stuckDetector = new StuckDetector(addonReader.PlayerReader, wowProcess, playerDirection, stopMoving, logger);
            var followRouteAction = new FollowRouteAction(addonReader.PlayerReader, wowProcess, playerDirection, pathPoints, stopMoving, NpcNameFinder, blacklist, logger, stuckDetector, classConfig);
            var walkToCorpseAction = new WalkToCorpseAction(addonReader.PlayerReader, wowProcess, playerDirection, spiritPath, pathPoints, stopMoving, logger, stuckDetector);

            this.RouteInfo = new RouteInfo(pathPoints, spiritPath, followRouteAction, walkToCorpseAction);

            availableActions.Clear();

            if (classConfig.Mode == Mode.CorpseRun)
            {
                availableActions.Add(new WaitAction(logger));
                availableActions.Add(new CorpseRunAction(addonReader.PlayerReader, wowProcess, playerDirection, spiritPath, stopMoving, logger, stuckDetector));
            }
            else if (classConfig.Mode == Mode.AttendedGather)
            {
                availableActions.Add(followRouteAction);
                availableActions.Add(new CorpseRunAction(addonReader.PlayerReader, wowProcess, playerDirection, spiritPath, stopMoving, logger, stuckDetector));
            }
            else
            {
                availableActions.Add(new ItemsBrokenAction(addonReader.PlayerReader, logger));

                if (classConfig.Mode == Mode.AttendedGrind)
                {
                    availableActions.Add(new WaitAction(logger));
                }
                else
                {
                    availableActions.Add(followRouteAction);
                    availableActions.Add(walkToCorpseAction);
                }
                availableActions.Add(new TargetDeadAction(wowProcess, logger));
                availableActions.Add(new ApproachTargetAction(wowProcess, addonReader.PlayerReader, stopMoving, logger, stuckDetector, classConfig));

                if (classConfig.WrongZone.ZoneId > 0)
                {
                    availableActions.Add(new WrongZoneAction(addonReader.PlayerReader, wowProcess, playerDirection, logger, stuckDetector, classConfig));
                }

                if (classConfig.Parallel.Sequence.Count > 0)
                {
                    availableActions.Add(new ParallelAction(wowProcess, addonReader.PlayerReader, stopMoving, classConfig.Parallel.Sequence, castingHandler, logger));
                }

                var lootAction = new LootAction(wowProcess, addonReader.PlayerReader, addonReader.BagReader, stopMoving, logger, classConfig);
                lootAction.AddPreconditions();
                availableActions.Add(lootAction);

                if (classConfig.Loot)
                {
                    lootAction = new PostKillLootAction(wowProcess, addonReader.PlayerReader, addonReader.BagReader, stopMoving, logger, classConfig);
                    lootAction.AddPreconditions();
                    availableActions.Add(lootAction);
                }

                try
                {
                    var genericCombat = new CombatAction(wowProcess, addonReader.PlayerReader, stopMoving, logger, classConfig, castingHandler);
                    availableActions.Add(genericCombat);
                    availableActions.Add(new PullTargetAction(wowProcess, addonReader.PlayerReader, NpcNameFinder, stopMoving, logger, castingHandler, stuckDetector, classConfig));

                    foreach (var item in classConfig.Adhoc.Sequence)
                    {
                        availableActions.Add(new AdhocAction(wowProcess, addonReader.PlayerReader, stopMoving, item, castingHandler, logger));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                }
            }

            return availableActions;
        }

        private static void GetPaths(out List<WowPoint> pathPoints, out List<WowPoint> spiritPath, ClassConfiguration classConfig)
        {
            if (!classConfig.PathFilename.Contains(":"))
            {
                classConfig.PathFilename = "../json/path/" + classConfig.PathFilename;
            }

            if (!classConfig.SpiritPathFilename.Contains(":"))
            {
                classConfig.SpiritPathFilename = "../json/path/" + classConfig.SpiritPathFilename;
            }

            string pathText = File.ReadAllText(classConfig.PathFilename);
            bool thereAndBack = classConfig.PathThereAndBack;
            if (string.IsNullOrEmpty(classConfig.SpiritPathFilename))
            {
                classConfig.SpiritPathFilename = classConfig.PathFilename;
            }
            string spiritText = File.ReadAllText(classConfig.SpiritPathFilename);
            int step = classConfig.PathReduceSteps ? 2 : 1;

            var pathPoints2 = JsonConvert.DeserializeObject<List<WowPoint>>(pathText);

            pathPoints = new List<WowPoint>();
            for (int i = 0; i < pathPoints2.Count; i += step)
            {
                if (i < pathPoints2.Count)
                {
                    pathPoints.Add(pathPoints2[i]);
                }
            }

            if (thereAndBack)
            {
                var reversePoints = pathPoints.ToList();
                reversePoints.Reverse();
                pathPoints.AddRange(reversePoints);
            }

            pathPoints.Reverse();
            spiritPath = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);
        }
    }
}