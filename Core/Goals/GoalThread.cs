using Core.GOAP;
using Microsoft.Extensions.Logging;
using SharedLib.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class GoalThread
    {
        private readonly ILogger logger;
        private readonly GoapAgent goapAgent;
        private readonly AddonReader addonReader;

        private GoapGoal? currentGoal;
        private RouteInfo? routeInfo;

        private bool active;
        public bool Active
        {
            get => active;
            set
            {
                active = value;
                if (!active)
                    goapAgent?.AvailableGoals.ToList().ForEach(goal => goal.OnActionEvent(this, new ActionEventArgs(GoapKey.abort, true)));

                if (goapAgent != null)
                    goapAgent.Active = active;
            }
        }

        public GoalThread(ILogger logger, GoapAgent goapAgent, AddonReader addonReader, RouteInfo? routeInfo)
        {
            this.logger = logger;
            this.goapAgent = goapAgent;
            this.addonReader = addonReader;
            this.routeInfo = routeInfo;
        }

        public void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.corpselocation && e.Value is CorpseLocation corpseLocation)
            {
                routeInfo?.PoiList.Add(new RouteInfoPoi(corpseLocation.WowPoint, "Corpse", "black", corpseLocation.Radius));
                //logger.LogInformation($"{nameof(GoalThread)} Kill location added to list");
            }
            else if (e.Key == GoapKey.consumecorpse && (bool)e.Value == false)
            {
                if (routeInfo != null && routeInfo.PoiList.Count > 0)
                {
                    var closest = routeInfo.PoiList.Where(p => p.Name == "Corpse").
                        Select(i => new { i, d = addonReader.PlayerReader.PlayerLocation.DistanceXYTo(i.Location) }).
                        Aggregate((a, b) => a.d <= b.d ? a : b);

                    if (closest.i != null)
                    {
                        routeInfo.PoiList.Remove(closest.i);
                    }
                }
            }
        }

        public void GoapPerformGoal()
        {
            if (goapAgent != null)
            {
                var newGoal = goapAgent.GetAction();
                if (newGoal != null)
                {
                    if (newGoal != currentGoal)
                    {
                        if (currentGoal != null)
                        {
                            try
                            {
                                currentGoal.OnExit();
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"OnExit on {currentGoal.Name}");
                            }
                        }

                        currentGoal = newGoal;

                        logger.LogInformation("---------------------------------");
                        logger.LogInformation($"New Plan= {newGoal.Name}");
                        
                        if (currentGoal != null)
                        {
                            try
                            {
                                currentGoal.OnEnter();
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"OnEnter on {currentGoal.Name}");
                            }
                        }
                    }

                    try
                    {
                        newGoal.PerformAction();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"PerformAction on {newGoal.Name}");
                    }
                }
                else
                {
                    //logger.LogInformation($"Current Plan= {currentGoal?.Name} -- New Plan= NULL");
                    Thread.Sleep(10);
                }
            }
        }

        public void ResumeIfNeeded()
        {
            currentGoal?.OnActionEvent(this, new ActionEventArgs(GoapKey.resume, true));
        }
    }
}