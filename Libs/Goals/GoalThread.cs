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
        private readonly PlayerReader playerReader;
        private readonly WowProcess wowProcess;
        private readonly GoapAgent goapAgent;

        private GoapGoal? currentGoal;
        public bool Active { get; set; }

        public GoalThread(PlayerReader playerReader, WowProcess wowProcess, GoapAgent goapAgent, ILogger logger)
        {
            this.playerReader = playerReader;
            this.wowProcess = wowProcess;
            this.goapAgent = goapAgent;
            this.logger = logger;
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