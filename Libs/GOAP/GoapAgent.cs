using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libs.Actions;
using Libs.NpcFinder;
using Microsoft.Extensions.Logging;

namespace Libs.GOAP
{
	public sealed class GoapAgent
	{
		private GoapPlanner planner;
		public IEnumerable<GoapAction> AvailableActions { get; set; }
		private PlayerReader playerReader;
		private ILogger logger;

		public GoapAction? CurrentAction { get; set; }
		public HashSet<KeyValuePair<GoapKey, object>> WorldState { get; set; } = new HashSet<KeyValuePair<GoapKey, object>>();
		private List<string> blacklist;

		public GoapAgent(PlayerReader playerReader, HashSet<GoapAction> availableActions, List<string> blacklist, ILogger logger)
		{
			this.playerReader = playerReader;
			this.AvailableActions = availableActions.OrderBy(a => a.CostOfPerformingAction);
			this.blacklist = blacklist;
			this.logger = logger;
			this.planner = new GoapPlanner(logger);
		}

		public async Task<GoapAction?> GetAction()
		{
			WorldState = await GetWorldState(playerReader);

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

		public async Task<HashSet<KeyValuePair<GoapKey, object>>> GetWorldState(PlayerReader playerReader)
		{
			if (blacklist.Contains(playerReader.Target))
			{
				await new WowProcess(logger).KeyPress(ConsoleKey.F3, 400);
			}

			var state= new HashSet<KeyValuePair<GoapKey, object>>
			{
				new KeyValuePair<GoapKey, object>(GoapKey.hastarget,!blacklist.Contains(playerReader.Target) && (!string.IsNullOrEmpty(playerReader.Target)|| playerReader.TargetHealth>0)),
				new KeyValuePair<GoapKey, object>(GoapKey.targetisalive, !playerReader.PlayerBitValues.TargetIsDead || playerReader.TargetHealth>0),
				new KeyValuePair<GoapKey, object>(GoapKey.incombat, playerReader.PlayerBitValues.PlayerInCombat ),
				new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, playerReader.WithInPullRange),
				new KeyValuePair<GoapKey, object>(GoapKey.inmeleerange, playerReader.WithInMeleeRange),
				new KeyValuePair<GoapKey, object>(GoapKey.pulled, false),
				new KeyValuePair<GoapKey, object>(GoapKey.shouldheal, playerReader.HealthPercent<60 && !playerReader.PlayerBitValues.DeadStatus),
				new KeyValuePair<GoapKey, object>(GoapKey.isdead, playerReader.HealthPercent==0),
				new KeyValuePair<GoapKey, object>(GoapKey.usehealingpotion, playerReader.HealthPercent<7)
			};

			actionState.ToList().ForEach(kv => state.Add(kv));

			return state;
		}

		public Dictionary<GoapKey, object> actionState = new Dictionary<GoapKey, object>();

		public void OnActionEvent(object sender, ActionEvent e)
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
