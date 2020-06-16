using Libs.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.GOAP
{
    public sealed class GoapAgent
    {
        private GoapPlanner planner;
        public IEnumerable<GoapAction> AvailableActions { get; set; }
        private PlayerReader playerReader;
        private ILogger logger;

        public GoapAction? CurrentAction { get; set; }
        public HashSet<KeyValuePair<GoapKey, object>> WorldState { get; private set; } = new HashSet<KeyValuePair<GoapKey, object>>();
        private IBlacklist blacklist;

        public GoapAgent(PlayerReader playerReader, HashSet<GoapAction> availableActions, IBlacklist blacklist, ILogger logger)
        {
            this.playerReader = playerReader;
            this.AvailableActions = availableActions.OrderBy(a => a.CostOfPerformingAction);
            this.blacklist = blacklist;
            this.logger = logger;
            this.planner = new GoapPlanner(logger);
        }

        public void UpdateWorldState()
        {
            WorldState = GetWorldState(playerReader);
        }

        public async Task<GoapAction?> GetAction()
        {
            if (playerReader.HealthPercent > 1 && blacklist.IsTargetBlacklisted())
            {
                logger.LogInformation("Target is blacklisted");
                await new WowProcess(logger).KeyPress(ConsoleKey.F3, 400);
                UpdateWorldState();
            }

            var goal = new HashSet<KeyValuePair<GoapKey, GoapPreCondition>>();

            //Plan
            Queue<GoapAction> plan = planner.Plan(AvailableActions, WorldState, goal);
            if (plan != null && plan.Count > 0)
            {
                CurrentAction = plan.Peek();
            }
            else
            {
                logger.LogInformation($"Target Health: {playerReader.TargetHealth}, max {playerReader.TargetMaxHealth}, dead {playerReader.PlayerBitValues.TargetIsDead}");

                await new WowProcess(logger).KeyPress(ConsoleKey.Tab, 420);
            }

            return CurrentAction;
        }

        private HashSet<KeyValuePair<GoapKey, object>> GetWorldState(PlayerReader playerReader)
        {
            var state = new HashSet<KeyValuePair<GoapKey, object>>
            {
                new KeyValuePair<GoapKey, object>(GoapKey.hastarget,!blacklist.IsTargetBlacklisted() && (!string.IsNullOrEmpty(playerReader.Target)|| playerReader.TargetHealth>0)),
                new KeyValuePair<GoapKey, object>(GoapKey.targetisalive,!string.IsNullOrEmpty(this.playerReader.Target) &&  (!playerReader.PlayerBitValues.TargetIsDead || playerReader.TargetHealth>0)),
                new KeyValuePair<GoapKey, object>(GoapKey.incombat, playerReader.PlayerBitValues.PlayerInCombat ),
                new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, playerReader.WithInPullRange),
                new KeyValuePair<GoapKey, object>(GoapKey.incombatrange, playerReader.WithInCombatRange),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, false),
                new KeyValuePair<GoapKey, object>(GoapKey.isdead, playerReader.HealthPercent==0),
            };

            actionState.ToList().ForEach(kv => state.Add(kv));

            return state;
        }

        private Dictionary<GoapKey, object> actionState = new Dictionary<GoapKey, object>();

        public void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (!actionState.ContainsKey(e.Key))
            {
                actionState.Add(e.Key, e.Value);
            }
            else
            {
                actionState[e.Key] = e.Value;
            }
        }
    }
}