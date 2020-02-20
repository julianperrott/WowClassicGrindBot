using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Libs.Actions;

namespace Libs.GOAP
{
	public sealed class GoapAgent
	{
		private GoapPlanner planner = new GoapPlanner();
		private HashSet<GoapAction> availableActions;
		private PlayerReader playerReader;

		public GoapAgent(PlayerReader playerReader, HashSet<GoapAction> availableActions)
		{
			this.playerReader = playerReader;
			this.availableActions = availableActions;
		}

		public GoapAction? GetAction()
		{
			var worldState = new HashSet<KeyValuePair<GoapKey, object>>
			{
				new KeyValuePair<GoapKey, object>(GoapKey.hastarget, !string.IsNullOrEmpty(playerReader.Target)|| playerReader.TargetHealth>0),
				new KeyValuePair<GoapKey, object>(GoapKey.targetisalive, !playerReader.PlayerBitValues.TargetIsDead || playerReader.TargetHealth>0),
				new KeyValuePair<GoapKey, object>(GoapKey.incombat, playerReader.PlayerBitValues.PlayerInCombat ),
				new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, playerReader.WithInPullRange),
				new KeyValuePair<GoapKey, object>(GoapKey.inmeleerange, playerReader.WithInMeleeRange),
				new KeyValuePair<GoapKey, object>(GoapKey.pulled, false),
				new KeyValuePair<GoapKey, object>(GoapKey.shouldheal, playerReader.HealthPercent<60 && !playerReader.PlayerBitValues.DeadStatus),
				new KeyValuePair<GoapKey, object>(GoapKey.isdead, playerReader.PlayerBitValues.DeadStatus),
			};

			//Debug.WriteLine(string.Join(", ",worldState.Select(k => k.Key + "=" + k.Value)));


			var goal = new HashSet<KeyValuePair<GoapKey, object>>();

			//Plan
			Queue<GoapAction> plan = planner.Plan(availableActions, worldState, goal);
			if (plan != null && plan.Count > 0)
			{
				return plan.Peek();
			}

			Debug.WriteLine($"Target Health: {playerReader.TargetHealth}, max {playerReader.TargetMaxHealth}, dead {playerReader.PlayerBitValues.TargetIsDead}");

			//new PressKeyThread(this.wowProcess, ConsoleKey.Tab);
			new WowProcess().SetKeyState(ConsoleKey.Tab, true);
			Thread.Sleep(420);
			new WowProcess().SetKeyState(ConsoleKey.Tab, false);

			return null;
		}
	}

}
