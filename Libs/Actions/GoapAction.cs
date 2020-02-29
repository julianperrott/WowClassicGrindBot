using Libs.GOAP;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public class GoapPreCondition
    {
        public string Description { get; private set; }
        public object State { get; private set; }

        public GoapPreCondition(string description, object state)
        {
            this.Description = description;
            this.State = state;
        }
    }

    public abstract class GoapAction
    {
        public HashSet<KeyValuePair<GoapKey, GoapPreCondition>> Preconditions { get; private set; } = new HashSet<KeyValuePair<GoapKey, GoapPreCondition>>();
        public HashSet<KeyValuePair<GoapKey, object>> Effects { get; private set; } = new HashSet<KeyValuePair<GoapKey, object>>();

        public bool InRangeOfTarget { get; set; }

        public abstract float CostOfPerformingAction { get; }

        public void DoReset()
        {
            ResetBeforePlanning();
        }

        public Dictionary<string, bool> State { get; set; } = new Dictionary<string, bool>();

        public virtual void ResetBeforePlanning() { }

        public virtual bool CheckIfActionCanRun() { return true; }

        public abstract Task PerformAction();

        public virtual async Task Abort() { await Task.Delay(0); }

        public void AddPrecondition(GoapKey key, object value)
        {
            var precondition = new GoapPreCondition(GoapKeyDescription.ToString(key, value), value);
            Preconditions.Add(new KeyValuePair<GoapKey, GoapPreCondition>(key, precondition));
        }

        public void RemovePrecondition(GoapKey key)
        {
            Preconditions.Where(o => o.Key.Equals(key))
              .ToList()
              .ForEach(o => Preconditions.Remove(o));
        }

        public void AddEffect(GoapKey key, object value)
        {
            Effects.Add(new KeyValuePair<GoapKey, object>(key, value));
        }

        public void RemoveEffect(GoapKey key)
        {
            Effects.Where(o => o.Key.Equals(key))
                .ToList()
                .ForEach(o => Effects.Remove(o));
        }
    }
}