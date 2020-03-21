using System;
using System.Collections.Generic;
using System.Text;

namespace Libs.GOAP
{
	public enum GoapKey
	{
		hastarget = 10,
		targetisalive = 20,
		incombat = 30,
		withinpullrange = 40,
		incombatrange = 50,
		pulled = 60,
		shouldheal = 70,
		isdead = 80,
		shouldloot = 90,
		usehealingpotion = 100,
		newtarget = 110,
		fighting = 120,
		bagfull= 130,
		abort=140,
		shoulddrink=150
	}

	public static class GoapKeyDescription
	{
		public static string ToString(GoapKey key, object state)
			 => (key, state) switch
			 {
				 (GoapKey.hastarget, true) => "Has a target",
				 (GoapKey.hastarget, false) => "Has no target",

				 (GoapKey.targetisalive, true) => "Target alive",
				 (GoapKey.targetisalive, false) => "Target dead",

				 (GoapKey.incombat, true) => "In combat",
				 (GoapKey.incombat, false) => "Out of combat",

				 (GoapKey.withinpullrange, true) => "In pull range",
				 (GoapKey.withinpullrange, false) => "Out of pull range",

				 (GoapKey.incombatrange, true) => "In combat range",
				 (GoapKey.incombatrange, false) => "Out of combat range",

				 (GoapKey.pulled, true) => "Pulled",
				 (GoapKey.pulled, false) => "Not pulled",

				 (GoapKey.shouldheal, true) => "Need to heal",
				 (GoapKey.shouldheal, false) => "Health ok",

				 (GoapKey.isdead, true) => "I am dead",
				 (GoapKey.isdead, false) => "I am alive",

				 (GoapKey.shouldloot, true) => "Need to loot",
				 (GoapKey.shouldloot, false) => "No need to loot",

				 (GoapKey.usehealingpotion, true) => "Use healing pot",
				 (GoapKey.usehealingpotion, false) => "My health is ok",

				 

				 (_, _) => "Unknown"
			 };
	}
}
