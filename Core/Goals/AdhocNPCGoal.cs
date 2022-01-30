using Core.GOAP;
using SharedLib.NpcFinder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using SharedLib.Extensions;

namespace Core.Goals
{
    public class AdhocNPCGoal : GoapGoal, IRouteProvider
    {
        private float RADIAN = MathF.PI * 2;

        private readonly ILogger logger;
        private readonly ConfigurableInput input;

        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly IPlayerDirection playerDirection;
        private readonly StopMoving stopMoving;
        private readonly StuckDetector stuckDetector;
        private readonly ClassConfiguration classConfiguration;
        private readonly NpcNameTargeting npcNameTargeting;
        private readonly IBlacklist blacklist;
        private readonly IPPather pather;
        private readonly MountHandler mountHandler;

        private readonly Wait wait;
        private readonly ExecGameCommand execGameCommand;
        private readonly GossipReader gossipReader;

        private readonly int GossipTimeout = 5000;

        private Stack<Vector3> routeToWaypoint = new Stack<Vector3>();

        public Stack<Vector3> PathingRoute()
        {
            return routeToWaypoint;
        }

        public bool HasNext()
        {
            return routeToWaypoint.Count != 0;
        }

        public Vector3 NextPoint()
        {
            return routeToWaypoint.Peek();
        }

        private float lastDistance = 999;
        public DateTime LastActive { get; set; } = DateTime.Now.AddDays(-1);
        private bool shouldMount = true;

        private readonly KeyAction key;

        public AdhocNPCGoal(ILogger logger, ConfigurableInput input, AddonReader addonReader, IPlayerDirection playerDirection, StopMoving stopMoving, NpcNameTargeting npcNameTargeting, StuckDetector stuckDetector, ClassConfiguration classConfiguration, IPPather pather, KeyAction key, IBlacklist blacklist, MountHandler mountHandler, Wait wait, ExecGameCommand exec)
        {
            this.logger = logger;
            this.input = input;
            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.playerDirection = playerDirection;
            this.stopMoving = stopMoving;
            this.npcNameTargeting = npcNameTargeting;

            this.stuckDetector = stuckDetector;
            this.classConfiguration = classConfiguration;
            this.pather = pather;
            this.key = key;
            this.blacklist = blacklist;
            this.mountHandler = mountHandler;

            this.wait = wait;
            this.execGameCommand = exec;
            this.gossipReader = addonReader.GossipReader;

            if (key.InCombat == "false")
            {
                AddPrecondition(GoapKey.incombat, false);
            }
            else if (key.InCombat == "true")
            {
                AddPrecondition(GoapKey.incombat, true);
            }

            this.Keys.Add(key);
        }

        public override float CostOfPerformingAction { get => key.Cost; }

        public override bool CheckIfActionCanRun()
        {
            return this.key.CanRun();
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (sender != this)
            {
                shouldMount = true;
                this.routeToWaypoint.Clear();
            }
        }

        public override async ValueTask PerformAction()
        {
            SendActionEvent(new ActionEventArgs(GoapKey.fighting, false));

            await Task.Delay(50);
            if (this.playerReader.Bits.PlayerInCombat && this.classConfiguration.Mode != Mode.AttendedGather) { return; }

            if ((DateTime.Now - LastActive).TotalSeconds > 10 || routeToWaypoint.Count == 0)
            {
                await FillRouteToDestination();
            }
            else
            {
                input.SetKeyState(input.ForwardKey, true, false, "NPC Goal 1");
            }

            var location = playerReader.PlayerLocation;
            var distance = location.DistanceXYTo(routeToWaypoint.Peek());
            var heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());

            AdjustHeading(heading);

            if (lastDistance < distance)
            {
                playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Further away");
            }
            else if (!this.stuckDetector.IsGettingCloser())
            {
                // stuck so jump
                input.SetKeyState(input.ForwardKey, true, false, "NPC Goal 2");
                await Task.Delay(100);
                if (HasBeenActiveRecently())
                {
                    await this.stuckDetector.Unstick();
                }
                else
                {
                    await Task.Delay(1000);
                    Log("Resuming movement");
                }
            }

            lastDistance = distance;

            if (distance < PointReachedDistance())
            {
                Log($"Move to next point");
                if (routeToWaypoint.Any())
                {
                    playerReader.ZCoord = routeToWaypoint.Peek().Z;
                    Log($"{nameof(AdhocNPCGoal)}: PlayerLocation.Z = {playerReader.PlayerLocation.Z}");
                }

                ReduceRoute();

                lastDistance = 999;
                if (routeToWaypoint.Count == 0)
                {
                    stopMoving.Stop();
                    distance = location.DistanceXYTo(StartOfPathToNPC());
                    if (distance > 50)
                    {
                        await FillRouteToDestination();
                    }

                    if (routeToWaypoint.Count == 0)
                    {
                        MoveCloserToPoint(400, StartOfPathToNPC());
                        MoveCloserToPoint(100, StartOfPathToNPC());

                        // we have reached the start of the path to the npc
                        FollowPath(this.key.Path);

                        stopMoving.Stop();
                        input.TapClearTarget();
                        wait.Update(1);

                        npcNameTargeting.ChangeNpcType(NpcNames.Friendly | NpcNames.Neutral);
                        npcNameTargeting.WaitForNUpdate(1);
                        bool foundVendor = npcNameTargeting.FindBy(CursorType.Vendor, CursorType.Repair, CursorType.Innkeeper);
                        if (!foundVendor)
                        {
                            LogWarn("Not found target by cursor. Attempt to use macro to aquire target");
                            input.KeyPress(key.ConsoleKey, input.defaultKeyPress);
                        }

                        (bool targetTimeout, double targetElapsedMs) = wait.Until(1000, () => playerReader.HasTarget);
                        if (targetTimeout)
                        {
                            LogWarn("No target found!");
                            return;
                        }

                        Log($"Found Target after {targetElapsedMs}ms");

                        if (!foundVendor)
                        {
                            input.TapInteractKey("Interact with target from macro");
                        }

                        OpenMerchantWindow();
                        input.TapClearTarget();

                        // walk back to the start of the path to the npc
                        var pathFrom = this.key.Path.ToList();
                        pathFrom.Reverse();
                        FollowPath(pathFrom);
                    }
                }

                if (routeToWaypoint.Count == 0)
                {
                    return;
                }

                this.stuckDetector.SetTargetLocation(this.routeToWaypoint.Peek());

                heading = DirectionCalculator.CalculateHeading(location, routeToWaypoint.Peek());
                playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Move to next point");
            }

            // should mount
            MountIfRequired();

            LastActive = DateTime.Now;
        }

        private void FollowPath(List<Vector3> path)
        {
            // show route on map
            this.routeToWaypoint.Clear();
            var rpath = path.ToList();
            rpath.Reverse();
            rpath.ForEach(p => this.routeToWaypoint.Push(p));

            if (mountHandler.IsMounted())
            {
                mountHandler.Dismount();
            }

            foreach (var point in path)
            {
                MoveCloserToPoint(400, point);
            }
        }

        private void MoveCloserToPoint(int pressDuration, Vector3 target)
        {
            Log($"Moving to spot = {target}");

            var distance = playerReader.PlayerLocation.DistanceXYTo(target);
            var lastDistance = distance;
            while (distance <= lastDistance && distance > 5)
            {
                if (this.playerReader.HealthPercent == 0) { return; }

                Log($"Distance to spot = {distance}");
                lastDistance = distance;
                var heading = DirectionCalculator.CalculateHeading(playerReader.PlayerLocation, target);
                playerDirection.SetDirection(heading, this.StartOfPathToNPC(), "Correcting direction", 0);

                this.input.KeyPress(input.ForwardKey, pressDuration);
                stopMoving.Stop();
                distance = playerReader.PlayerLocation.DistanceXYTo(target);
            }
        }

        private void MountIfRequired()
        {
            if (shouldMount && !mountHandler.IsMounted() && !playerReader.Bits.PlayerInCombat)
            {
                shouldMount = false;

                mountHandler.MountUp();

                input.SetKeyState(input.ForwardKey, true, false, "Move forward");
            }
        }

        private void ReduceRoute()
        {
            if (routeToWaypoint.Any())
            {
                var location = playerReader.PlayerLocation;
                var distance = location.DistanceXYTo(routeToWaypoint.Peek());
                while (distance < PointReachedDistance() && routeToWaypoint.Any())
                {
                    routeToWaypoint.Pop();
                    if (routeToWaypoint.Any())
                    {
                        distance = location.DistanceXYTo(routeToWaypoint.Peek());
                    }
                }
            }
        }

        private async ValueTask FillRouteToDestination()
        {
            this.routeToWaypoint.Clear();
            Vector3 target = StartOfPathToNPC();
            var location = playerReader.PlayerLocation;
            var heading = DirectionCalculator.CalculateHeading(location, target);
            playerDirection.SetDirection(heading, target, "Set Location target");

            // create route to vendo
            stopMoving.Stop();
            var path = await this.pather.FindRouteTo(addonReader, target);
            path.Reverse();
            path.ForEach(p => this.routeToWaypoint.Push(p));

            if (routeToWaypoint.Any())
            {
                playerReader.ZCoord = routeToWaypoint.Peek().Z;
                Log($"{nameof(AdhocNPCGoal)}: PlayerLocation.Z = {playerReader.PlayerLocation.Z}");
            }

            this.ReduceRoute();
            if (this.routeToWaypoint.Count == 0)
            {
                this.routeToWaypoint.Push(target);
            }

            this.stuckDetector.SetTargetLocation(this.routeToWaypoint.Peek());
        }

        private Vector3 StartOfPathToNPC()
        {
            if (!this.key.Path.Any())
            {
                this.logger.LogError("Path to target is not defined");
                throw new Exception("Path to target is not defined");
            }

            return this.key.Path[0];
        }

        private void AdjustHeading(float heading)
        {
            var diff1 = MathF.Abs(RADIAN + heading - playerReader.Direction) % RADIAN;
            var diff2 = MathF.Abs(heading - playerReader.Direction - RADIAN) % RADIAN;

            var wanderAngle = 0.3;

            if (this.classConfiguration.Mode != Mode.AttendedGather)
            {
                wanderAngle = 0.05;
            }

            if (MathF.Min(diff1, diff2) > wanderAngle)
            {
                Log("Correct direction");
                playerDirection.SetDirection(heading, routeToWaypoint.Peek(), "Correcting direction");
            }
            else
            {
                Log($"Direction ok heading: {heading}, player direction {playerReader.Direction}");
            }
        }

        private int PointReachedDistance()
        {
            return mountHandler.IsMounted() ? 25 : 9;
        }

        private bool HasBeenActiveRecently()
        {
            return (DateTime.Now - LastActive).TotalSeconds < 2;
        }

        public override string Name => this.Keys.Count == 0 ? base.Name : this.Keys[0].Name;

        private bool OpenMerchantWindow()
        {
            (bool timeout, double elapsedMs) = wait.Until(GossipTimeout, () => gossipReader.GossipStart || gossipReader.MerchantWindowOpened);
            if (gossipReader.MerchantWindowOpened)
            {
                LogWarn($"Gossip no options! {elapsedMs}ms");
            }
            else
            {
                (bool gossipEndTimeout, double gossipEndElapsedMs) = wait.Until(GossipTimeout, () => gossipReader.GossipEnd);
                if (timeout)
                {
                    LogWarn($"Gossip too many options? {gossipEndElapsedMs}ms");
                    return false;
                }
                else
                {
                    if (gossipReader.Gossips.TryGetValue(Gossip.Vendor, out int orderNum))
                    {
                        Log($"Picked {orderNum}th for {Gossip.Vendor}");
                        execGameCommand.Run($"/run SelectGossipOption({orderNum})--");
                    }
                    else
                    {
                        LogWarn($"Target({playerReader.TargetId}) has no {Gossip.Vendor} option!");
                        return false;
                    }
                }
            }

            Log($"Merchant window opened after {elapsedMs}ms");

            (bool sellStartedTimeout, double sellStartedElapsedMs) = wait.Until(GossipTimeout, () => gossipReader.MerchantWindowSelling);
            if (!sellStartedTimeout)
            {
                Log($"Merchant sell grey items started after {sellStartedElapsedMs}ms");

                (bool sellFinishedTimeout, double sellFinishedElapsedMs) = wait.Until(GossipTimeout, () => gossipReader.MerchantWindowSellingFinished);
                if (!sellFinishedTimeout)
                {
                    Log($"Merchant sell grey items finished, took {sellFinishedElapsedMs}ms");
                }
                else
                {
                    Log($"Merchant sell grey items timeout! Too many items to sell?! Increase {nameof(GossipTimeout)} - {sellFinishedElapsedMs}ms");
                    return true;
                }
            }
            else
            {
                Log($"Merchant sell nothing! {sellStartedElapsedMs}ms");
                return true;
            }

            return false;
        }

        private void Log(string text)
        {
            logger.LogInformation($"[{nameof(AdhocNPCGoal)}]: {text}");
        }

        private void LogWarn(string text)
        {
            logger.LogWarning($"[{nameof(AdhocNPCGoal)}]: {text}");
        }
    }
}