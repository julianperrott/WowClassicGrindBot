using Libs.Goals;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Libs
{
    public class GoalFactory
    {
        private readonly AddonReader addonReader;
        public NpcNameFinder NpcNameFinder { get; private set; }
        private readonly WowProcess wowProcess;
        private readonly IPPather pather;
        private ILogger logger;

        public bool PotentialAddsExist => NpcNameFinder.PotentialAddsExist;

        public RouteInfo? RouteInfo { get; private set; }

        public GoalFactory(AddonReader addonReader, ILogger logger, WowProcess wowProcess, NpcNameFinder npcNameFinder, IPPather pather)
        {
            this.logger = logger;
            this.addonReader = addonReader;
            this.NpcNameFinder = npcNameFinder;
            this.wowProcess = wowProcess;
            this.pather = pather;
        }

        public HashSet<GoapGoal> CreateGoals(ClassConfiguration classConfig, IBlacklist blacklist)
        {
            var availableActions = new HashSet<GoapGoal>();

            List<WowPoint> pathPoints, spiritPath;
            GetPaths(out pathPoints, out spiritPath, classConfig);

            var playerDirection = new PlayerDirection(addonReader.PlayerReader, wowProcess, logger);
            var stopMoving = new StopMoving(wowProcess, addonReader.PlayerReader);

            var castingHandler = new CastingHandler(wowProcess, addonReader.PlayerReader, logger, classConfig, playerDirection, NpcNameFinder);

            var stuckDetector = new StuckDetector(addonReader.PlayerReader, wowProcess, playerDirection, stopMoving, logger);
            var followRouteAction = new FollowRouteGoal(addonReader.PlayerReader, wowProcess, playerDirection, pathPoints, stopMoving, NpcNameFinder, blacklist, logger, stuckDetector, classConfig, pather);
            var walkToCorpseAction = new WalkToCorpseGoal(addonReader.PlayerReader, wowProcess, playerDirection, spiritPath, pathPoints, stopMoving, logger, stuckDetector, pather);

            availableActions.Clear();

            if (classConfig.Mode == Mode.CorpseRun)
            {
                availableActions.Add(new WaitGoal(logger));
                availableActions.Add(new CorpseRunGoal(addonReader.PlayerReader, wowProcess, playerDirection, spiritPath, stopMoving, logger, stuckDetector));
            }
            else if (classConfig.Mode == Mode.AttendedGather)
            {
                availableActions.Add(followRouteAction);
                availableActions.Add(new CorpseRunGoal(addonReader.PlayerReader, wowProcess, playerDirection, spiritPath, stopMoving, logger, stuckDetector));
            }
            else
            {
                if (classConfig.Mode == Mode.AttendedGrind)
                {
                    availableActions.Add(new WaitGoal(logger));
                }
                else
                {
                    availableActions.Add(followRouteAction);
                    availableActions.Add(walkToCorpseAction);
                }
                availableActions.Add(new TargetDeadGoal(wowProcess, logger));
                availableActions.Add(new ApproachTargetGoal(wowProcess, addonReader.PlayerReader, stopMoving, logger, stuckDetector, classConfig));

                if (classConfig.WrongZone.ZoneId > 0)
                {
                    availableActions.Add(new WrongZoneGoal(addonReader.PlayerReader, wowProcess, playerDirection, logger, stuckDetector, classConfig));
                }

                if (classConfig.Parallel.Sequence.Count > 0)
                {
                    availableActions.Add(new ParallelGoal(wowProcess, addonReader.PlayerReader, stopMoving, classConfig.Parallel.Sequence, castingHandler, logger));
                }

                var lootAction = new LootGoal(wowProcess, addonReader.PlayerReader, addonReader.BagReader, stopMoving, logger, classConfig);
                lootAction.AddPreconditions();
                availableActions.Add(lootAction);

                if (classConfig.Loot)
                {
                    lootAction = new PostKillLootGoal(wowProcess, addonReader.PlayerReader, addonReader.BagReader, stopMoving, logger, classConfig);
                    lootAction.AddPreconditions();
                    availableActions.Add(lootAction);
                }

                try
                {
                    var genericCombat = new CombatGoal(wowProcess, addonReader.PlayerReader, stopMoving, logger, classConfig, castingHandler);
                    availableActions.Add(genericCombat);
                    availableActions.Add(new PullTargetGoal(wowProcess, addonReader.PlayerReader, NpcNameFinder, stopMoving, logger, castingHandler, stuckDetector, classConfig));

                    foreach (var item in classConfig.Adhoc.Sequence)
                    {
                        availableActions.Add(new AdhocGoal(wowProcess, addonReader.PlayerReader, stopMoving, item, castingHandler, logger));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                }

                var pathProviders = new List<IRouteProvider>
                {
                    followRouteAction,
                    walkToCorpseAction
                };

                if (classConfig.VendorLocation.X > 0 && !string.IsNullOrEmpty(classConfig.VendorTargetKey))
                {
                    var vendorAction = new VendorGoal(addonReader.PlayerReader, wowProcess, playerDirection, stopMoving, logger, stuckDetector, classConfig, pather, this.addonReader.BagReader);
                    availableActions.Add(vendorAction);
                    pathProviders.Add(vendorAction);
                }
                else
                {
                    logger.LogWarning("Vendor location or target key is not defined, so no vendoring when bags are full.");
                }


                if (classConfig.RepairLocation.X > 0 && !string.IsNullOrEmpty(classConfig.RepairTargetKey))
                {
                    var repairAction = new RepairGoal(addonReader.PlayerReader, wowProcess, playerDirection, stopMoving, logger, stuckDetector, classConfig, pather, this.addonReader.BagReader);
                    availableActions.Add(repairAction);
                    pathProviders.Add(repairAction);
                }
                else
                {
                    availableActions.Add(new ItemsBrokenGoal(addonReader.PlayerReader, logger));
                    logger.LogWarning("Repair location or target key is not defined, so bot will stop if gear is red.");
                }

                this.RouteInfo = new RouteInfo(pathPoints, spiritPath, pathProviders, addonReader.PlayerReader);
            }

            return availableActions;
        }

        private static void GetPaths(out List<WowPoint> pathPoints, out List<WowPoint> spiritPath, ClassConfiguration classConfig)
        {
            if (!classConfig.PathFilename.Contains(":"))
            {
                classConfig.PathFilename = "../json/path/" + classConfig.PathFilename;
            }

            if (!classConfig.SpiritPathFilename.Contains(":") && !string.IsNullOrEmpty(classConfig.SpiritPathFilename))
            {
                classConfig.SpiritPathFilename = "../json/path/" + classConfig.SpiritPathFilename;
            }

            string pathText = File.ReadAllText(classConfig.PathFilename);
            bool thereAndBack = classConfig.PathThereAndBack;
            
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

            if (string.IsNullOrEmpty(classConfig.SpiritPathFilename))
            {
                spiritPath = new List<WowPoint> { pathPoints.First() };
            }
            else
            {
                string spiritText = File.ReadAllText(classConfig.SpiritPathFilename);
                spiritPath = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);
            }
        }
    }
}