using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libs.Actions;

namespace Libs.GOAP
{
	public sealed class GoapAgent
	{
		private GoapPlanner planner = new GoapPlanner();
		public IEnumerable<GoapAction> AvailableActions { get; set; }
		private PlayerReader playerReader;

		public GoapAction? CurrentAction { get; set; }
		public HashSet<KeyValuePair<GoapKey, object>> WorldState { get; set; } = new HashSet<KeyValuePair<GoapKey, object>>();

		public GoapAgent(PlayerReader playerReader, HashSet<GoapAction> availableActions)
		{
			this.playerReader = playerReader;
			this.AvailableActions = availableActions.OrderBy(a => a.CostOfPerformingAction);
		}

		public async Task<GoapAction?> GetAction()
		{
			WorldState = GetWorldState(playerReader);

			var goal = new HashSet<KeyValuePair<GoapKey, GoapPreCondition>>();

			//Plan
			Queue<GoapAction> plan = planner.Plan(AvailableActions, WorldState, goal);
			if (plan != null && plan.Count > 0)
			{
				CurrentAction = plan.Peek();
			}
			else
			{
				Debug.WriteLine($"Target Health: {playerReader.TargetHealth}, max {playerReader.TargetMaxHealth}, dead {playerReader.PlayerBitValues.TargetIsDead}");

				await new WowProcess().KeyPress(ConsoleKey.Tab, 420);
			}

			return CurrentAction;
		}

		public HashSet<KeyValuePair<GoapKey, object>> GetWorldState(PlayerReader playerReader)
		{
			var state= new HashSet<KeyValuePair<GoapKey, object>>
			{
				new KeyValuePair<GoapKey, object>(GoapKey.hastarget, !string.IsNullOrEmpty(playerReader.Target)|| playerReader.TargetHealth>0),
				new KeyValuePair<GoapKey, object>(GoapKey.targetisalive, !playerReader.PlayerBitValues.TargetIsDead || playerReader.TargetHealth>0),
				new KeyValuePair<GoapKey, object>(GoapKey.incombat, playerReader.PlayerBitValues.PlayerInCombat ),
				new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, playerReader.WithInPullRange),
				new KeyValuePair<GoapKey, object>(GoapKey.inmeleerange, playerReader.WithInMeleeRange),
				new KeyValuePair<GoapKey, object>(GoapKey.pulled, false),
				new KeyValuePair<GoapKey, object>(GoapKey.shouldheal, playerReader.HealthPercent<60 && !playerReader.PlayerBitValues.DeadStatus),
				new KeyValuePair<GoapKey, object>(GoapKey.isdead, playerReader.HealthPercent==0),
				new KeyValuePair<GoapKey, object>(GoapKey.usehealingpotion, playerReader.HealthPercent<20)
			};

			actionState.ToList().ForEach(kv => state.Add(kv));

			return state;
		}

		public Dictionary<GoapKey, object> actionState = new Dictionary<GoapKey, object>();

		public void OnActionEvent(object sender, ActionEvent e)
		{
			if (e.Key == GoapKey.newtarget)
			{
				var killAction = AvailableActions.Where(a => a.GetType() == typeof(KillTargetAction)).FirstOrDefault() as KillTargetAction;
				killAction?.ResetRend();
			}
			else
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
}
