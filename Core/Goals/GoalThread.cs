using Core.GOAP;
using Microsoft.Extensions.Logging;
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

        private GoapGoal? currentGoal;
        private RouteInfo? routeInfo;

        private bool active;
        public bool Active
        {
            get => active;
            set
            {
                active = value;

                if(!active)
                    goapAgent?.AvailableGoals.ToList().ForEach(goal => goal.OnActionEvent(this, new ActionEventArgs(GoapKey.abort, true)));
            }
        }

        public GoalThread(ILogger logger, GoapAgent goapAgent, RouteInfo? routeInfo)
        {
            this.logger = logger;
            this.goapAgent = goapAgent;
            this.routeInfo = routeInfo;
        }

        public void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.corpselocation && e.Value is CorpseLocation corpseLocation)
            {
                routeInfo?.PoiList.Add(new RouteInfoPoi(corpseLocation.WowPoint, "Corpse", "black", corpseLocation.Radius));
                logger.LogInformation($"{GetType().Name} Corpse added to list");
            }
            else if (e.Key == GoapKey.consumecorpse && (bool)e.Value == false)
            {
                if (routeInfo != null && routeInfo.PoiList.Any())
                {
                    var closest = routeInfo.PoiList.Where(p => p.Name == "Corpse").
                        Min(i => (WowPoint.DistanceTo(goapAgent.PlayerReader.PlayerLocation, i.Location), i));
                    if (closest.i != null)
                    {
                        routeInfo.PoiList.Remove(closest.i);
                    }
                }
            }
        }

        public async Task GoapPerformGoal()
        {
            if (this.goapAgent != null)
            {
                var newGoal = await this.goapAgent.GetAction();

                if (newGoal != null)
                {
                    if (newGoal != this.currentGoal)
                    {
                        if (this.currentGoal != null)
                        {
                            try
                            {
                                await this.currentGoal.OnExit();
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"OnExit on {currentGoal.GetType().Name}");
                            }
                        }

                        this.currentGoal?.DoReset();
                        this.currentGoal = newGoal;

                        logger.LogInformation("---------------------------------");
                        logger.LogInformation($"New Plan= {newGoal.GetType().Name}");
                        
                        if (currentGoal != null)
                        {
                            try
                            {
                                await this.currentGoal.OnEnter();
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"OnEnter on {newGoal.GetType().Name}");
                            }
                        }
                    }

                    try
                    {
                        await newGoal.PerformAction();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"PerformAction on {newGoal.GetType().Name}");
                    }
                }
                else
                {
                    logger.LogInformation($"New Plan= NULL");
                    Thread.Sleep(50);
                }
            }
        }

        public void ResumeIfNeeded()
        {
            currentGoal?.OnActionEvent(this, new ActionEventArgs(GoapKey.resume, true));
        }
    }
}