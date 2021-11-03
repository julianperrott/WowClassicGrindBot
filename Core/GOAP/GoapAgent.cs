using Core.Goals;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.GOAP
{
    public sealed class GoapAgent
    {
        private readonly ILogger logger;
        private readonly ConfigurableInput input;
        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly StopMoving stopMoving;
        private readonly IBlacklist blacklist;
        private readonly GoapPlanner planner;

        public GoapAgentState GoapAgentState { private set; get; }

        public IEnumerable<GoapGoal> AvailableGoals { get; set; }
        public GoapGoal? CurrentGoal { get; set; }

        private Dictionary<GoapKey, object> actionState = new Dictionary<GoapKey, object>();
        public HashSet<KeyValuePair<GoapKey, object>> WorldState { get; private set; } = new HashSet<KeyValuePair<GoapKey, object>>();

        public GoapAgent(ILogger logger, GoapAgentState goapAgentState, ConfigurableInput input, AddonReader addonReader, HashSet<GoapGoal> availableGoals, IBlacklist blacklist)
        {
            this.logger = logger;
            this.GoapAgentState = goapAgentState;
            this.input = input;

            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;

            this.addonReader.CreatureHistory.KillCredit -= OnKillCredit;
            this.addonReader.CreatureHistory.KillCredit += OnKillCredit;

            this.stopMoving = new StopMoving(input, playerReader);

            this.AvailableGoals = availableGoals.OrderBy(a => a.CostOfPerformingAction);
            this.blacklist = blacklist;

            this.planner = new GoapPlanner(logger);
        }

        public void UpdateWorldState()
        {
            WorldState = GetWorldState();
        }

        public async Task<GoapGoal?> GetAction()
        {
            if (playerReader.HealthPercent > 1 && blacklist.IsTargetBlacklisted())
            {
                logger.LogWarning($"{GetType().Name}: Target is blacklisted - StopAttack & ClearTarget");
                await input.TapStopAttack("");
                await input.TapClearTarget("");
                UpdateWorldState();
            }

            var goal = new HashSet<KeyValuePair<GoapKey, GoapPreCondition>>();

            //Plan
            Queue<GoapGoal> plan = planner.Plan(AvailableGoals, WorldState, goal);
            if (plan != null && plan.Count > 0)
            {
                if (CurrentGoal == plan.Peek() && !CurrentGoal.Repeatable)
                {
                    CurrentGoal = null;
                }
                else
                {
                    CurrentGoal = plan.Peek();
                }
            }
            else
            {
                if (CurrentGoal != null && !CurrentGoal.Repeatable)
                {
                    logger.LogInformation($"Plan= {CurrentGoal.GetType().Name} is not Repeatable!");
                    CurrentGoal = null;

                    await stopMoving.Stop();
                }
            }

            return CurrentGoal;
        }

        private HashSet<KeyValuePair<GoapKey, object>> GetWorldState()
        {
            var state = new HashSet<KeyValuePair<GoapKey, object>>
            {
                new KeyValuePair<GoapKey, object>(GoapKey.hastarget, !blacklist.IsTargetBlacklisted() && playerReader.HasTarget),
                new KeyValuePair<GoapKey, object>(GoapKey.dangercombat, addonReader.CombatCreatureCount > 0),
                new KeyValuePair<GoapKey, object>(GoapKey.pethastarget, playerReader.PetHasTarget),
                new KeyValuePair<GoapKey, object>(GoapKey.targetisalive, !string.IsNullOrEmpty(this.playerReader.Target) && (!playerReader.Bits.TargetIsDead || playerReader.TargetHealth>0)),
                new KeyValuePair<GoapKey, object>(GoapKey.incombat, playerReader.Bits.PlayerInCombat),
                new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, playerReader.WithInPullRange),
                new KeyValuePair<GoapKey, object>(GoapKey.incombatrange, playerReader.WithInCombatRange),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, false),
                new KeyValuePair<GoapKey, object>(GoapKey.isdead, playerReader.HealthPercent==0),
                new KeyValuePair<GoapKey, object>(GoapKey.isswimming, playerReader.Bits.IsSwimming),
                new KeyValuePair<GoapKey, object>(GoapKey.itemsbroken, playerReader.Bits.ItemsAreBroken),
                new KeyValuePair<GoapKey, object>(GoapKey.producedcorpse, GoapAgentState.LastCombatKillCount > 0),

                // these hold their state
                new KeyValuePair<GoapKey, object>(GoapKey.consumecorpse, GoapAgentState.ShouldConsumeCorpse),
                new KeyValuePair<GoapKey, object>(GoapKey.shouldloot, GoapAgentState.NeedLoot),
                new KeyValuePair<GoapKey, object>(GoapKey.shouldskin, GoapAgentState.NeedSkin)
        };

            actionState.ToList().ForEach(kv => state.Add(kv));

            return state;
        }

        public void OnActionEvent(object sender, ActionEventArgs e)
        {
            switch (e.Key)
            {
                case GoapKey.shouldloot:
                    GoapAgentState.NeedLoot = (bool)e.Value;
                    break;
                case GoapKey.shouldskin:
                    GoapAgentState.NeedSkin = (bool)e.Value;
                    break;
            }

            if (!actionState.ContainsKey(e.Key))
            {
                actionState.Add(e.Key, e.Value);
            }
            else
            {
                actionState[e.Key] = e.Value;
            }
        }

        private void OnKillCredit(object obj, EventArgs e)
        {
            GoapAgentState.IncrementKillCount();

            if (CurrentGoal == null)
            {
                AvailableGoals.ToList().ForEach(x => x.OnActionEvent(this, new ActionEventArgs(GoapKey.producedcorpse, true)));
            }
            else
            {
                CurrentGoal.OnActionEvent(this, new ActionEventArgs(GoapKey.producedcorpse, true));
            }

            logger.LogInformation($"{GetType().Name} --- Kill credit detected! Known kills: {GoapAgentState.LastCombatKillCount} | Combat mobs remaing: {addonReader.CombatCreatureCount}");
        }
    }
}