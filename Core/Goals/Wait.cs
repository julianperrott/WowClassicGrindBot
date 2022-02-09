using System;
using System.Threading;

namespace Core
{
    public class Wait
    {
        private readonly AddonReader addonReader;
        private readonly AutoResetEvent autoResetEvent;

        public Wait(AddonReader addonReader, AutoResetEvent autoResetEvent)
        {
            this.addonReader = addonReader;
            this.autoResetEvent = autoResetEvent;
        }

        public void Update(int n)
        {
            int s = addonReader.GlobalTime.Value;
            while (Math.Abs(s - addonReader.GlobalTime.Value) <= n)
            {
                autoResetEvent.WaitOne();
            }
        }

        public bool Till(int timeoutMs, Func<bool> interrupt)
        {
            DateTime start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
            {
                Update(1);
                if (interrupt())
                    return false;
            }

            return true;
        }

        public (bool timeout, double elapsedMs) Until(int timeoutMs, Func<bool> interrupt, Action? repeat = null)
        {
            DateTime start = DateTime.UtcNow;
            double elapsedMs;
            while ((elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds) < timeoutMs)
            {
                repeat?.Invoke();
                Update(1);
                if (interrupt())
                    return (false, elapsedMs);
            }

            return (true, elapsedMs);
        }

        public void While(Func<bool> condition)
        {
            while (condition())
            {
                Update(1);
            }
        }
    }
}
