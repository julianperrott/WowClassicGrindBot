using Libs.GOAP;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Actions
{
    public abstract class GoapAction
    {
        public HashSet<KeyValuePair<GoapKey, object>> Preconditions { get; private set; } = new HashSet<KeyValuePair<GoapKey, object>>();
        public HashSet<KeyValuePair<GoapKey, object>> Effects { get; private set; } = new HashSet<KeyValuePair<GoapKey, object>>();

        public bool InRangeOfTarget { get; set; }

        public abstract float CostOfPerformingAction { get; }

        public void DoReset()
        {
            ResetBeforePlanning();
        }

        public abstract void ResetBeforePlanning();

        public abstract bool IsActionDone();

        public abstract bool CheckIfActionCanRun();

        public abstract Task PerformAction();

        public abstract bool NeedsToBeInRangeOfTargetToExecute();

        public virtual void Abort() { }

        public void AddPrecondition(GoapKey key, object value)
        {
            Preconditions.Add(new KeyValuePair<GoapKey, object>(key, value));
        }

        public void RemovePrecondition(GoapKey key)
        {
            Remove(key, Preconditions);
        }

        public void AddEffect(GoapKey key, object value)
        {
            Effects.Add(new KeyValuePair<GoapKey, object>(key, value));
        }

        public void RemoveEffect(GoapKey key)
        {
            Remove(key, Effects);
        }

        private void Remove(GoapKey key, HashSet<KeyValuePair<GoapKey, object>> hash)
        {
            hash.Where(o => o.Key.Equals(key))
                .ToList()
                .ForEach(o => hash.Remove(o));
        }
    }
}