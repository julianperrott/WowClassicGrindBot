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
        private readonly ConfigurableInput input;

        private readonly PlayerReader playerReader;
        private readonly GoapAgent goapAgent;

        private GoapGoal? currentGoal;

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

        public GoalThread(ILogger logger, ConfigurableInput input, PlayerReader playerReader, GoapAgent goapAgent)
        {
            this.logger = logger;
            this.input = input;
            this.playerReader = playerReader;
            this.goapAgent = goapAgent;
        }

        public void OnActionEvent(object sender, ActionEventArgs e)
        {
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