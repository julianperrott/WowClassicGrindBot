using Libs.Goals;
using Libs.PPather;
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
        private readonly ILogger logger;
        private readonly WowProcess wowProcess;
        private readonly WowInput wowInput;

        private readonly AddonReader addonReader;
        private readonly NpcNameFinder npcNameFinder;
        private readonly IPPather pather;

        public RouteInfo? RouteInfo { get; private set; }

        public GoalFactory(ILogger logger, AddonReader addonReader, WowProcess wowProcess, WowInput wowInput, NpcNameFinder npcNameFinder, IPPather pather)
        {
            this.logger = logger;
            this.addonReader = addonReader;
            this.wowProcess = wowProcess;
            this.wowInput = wowInput;
            this.npcNameFinder = npcNameFinder;
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


                foreach (var item in classConfig.NPC.Sequence)
                {
                    availableActions.Add(new AdhocNPCGoal(addonReader.PlayerReader, wowProcess, playerDirection, stopMoving, NpcNameFinder, logger, stuckDetector, classConfig, pather, item, blacklist));
                    item.Path.AddRange(ReadPath(item.Name, item.PathFilename));
                }

                var pathProviders = availableActions.Where(a => a is IRouteProvider)
                    .Cast<IRouteProvider>()
                    .ToList();

                this.RouteInfo = new RouteInfo(pathPoints, spiritPath, pathProviders, addonReader.PlayerReader);

                this.pather.DrawLines(new List<LineArgs>()
                {
                      new LineArgs  { Spots = pathPoints, Name = "grindpath", Colour = 2, MapId = addonReader.PlayerReader.ZoneId },
                      new LineArgs { Spots = spiritPath, Name = "spirithealer", Colour = 3, MapId = addonReader.PlayerReader.ZoneId }
                });
            }

            return availableActions;
        }

        private static string FixPathFilename(string path)
        {
            if (!path.Contains(":") && !string.IsNullOrEmpty(path))
            {
                return "../json/path/" + path;
            }
            return path;
        }

        private static void GetPaths(out List<WowPoint> pathPoints, out List<WowPoint> spiritPath, ClassConfiguration classConfig)
        {
            classConfig.PathFilename = FixPathFilename(classConfig.PathFilename);
            classConfig.SpiritPathFilename = FixPathFilename(classConfig.SpiritPathFilename);

            pathPoints = CreatePathPoints(classConfig);
            spiritPath = CreateSpiritPathPoints(pathPoints, classConfig);
        }

        private IEnumerable<WowPoint> ReadPath(string name, string pathFilename)
        {
            try
            {
                if (string.IsNullOrEmpty(pathFilename))
                {
                    return new List<WowPoint>();
                }
                else
                {
                    return JsonConvert.DeserializeObject<List<WowPoint>>(File.ReadAllText(FixPathFilename(pathFilename)));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Reading path: {name}");
                throw;
            }
        }

        private static List<WowPoint> CreateSpiritPathPoints(List<WowPoint> pathPoints, ClassConfiguration classConfig)
        {
            List<WowPoint> spiritPath;
            if (string.IsNullOrEmpty(classConfig.SpiritPathFilename))
            {
                spiritPath = new List<WowPoint> { pathPoints.First() };
            }
            else
            {
                string spiritText = File.ReadAllText(classConfig.SpiritPathFilename);
                spiritPath = JsonConvert.DeserializeObject<List<WowPoint>>(spiritText);
            }

            return spiritPath;
        }

        private static List<WowPoint> CreatePathPoints(ClassConfiguration classConfig)
        {
            List<WowPoint> pathPoints;
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
            return pathPoints;
        }
    }
}