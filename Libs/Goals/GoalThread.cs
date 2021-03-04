using Libs.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Libs.Goals
{
    public class GoalThread
    {
        private readonly ILogger logger;
        private readonly WowInput wowInput;

        private readonly PlayerReader playerReader;
        private readonly GoapAgent goapAgent;

        private GoapGoal? currentGoal;
        public bool Active { get; set; }

        public GoalThread(ILogger logger, WowInput wowInput, PlayerReader playerReader, GoapAgent goapAgent)
        {
            this.logger = logger;
            this.wowInput = wowInput;
            this.playerReader = playerReader;
            this.goapAgent = goapAgent;
            
        }

        public void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.abort)
            {
                logger.LogInformation($"Abort from: {sender.GetType().Name}");

                var location = this.playerReader.PlayerLocation;
                wowProcess?.Hearthstone();
                Active = false;
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
                        this.currentGoal?.DoReset();
                        this.currentGoal = newGoal;
                        logger.LogInformation("---------------------------------");
                        logger.LogInformation($"New Plan= {newGoal.GetType().Name}");
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
                    Thread.Sleep(500);
                }
            }
        }
    }
}