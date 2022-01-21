using System;
using System.Threading.Tasks;

namespace Core
{
    public class Wait
    {
        private readonly AddonReader addonReader;

        public Wait(AddonReader addonReader)
        {
            this.addonReader = addonReader;
        }

        public async ValueTask Update(int n)
        {
            int s = addonReader.GlobalTime.Value;
            while (Math.Abs(s - addonReader.GlobalTime.Value) <= n)
            {
                await Task.Delay(2);
            }
        }


        public async ValueTask<bool> Interrupt(int durationMs, Func<bool> interrupt)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < durationMs)
            {
                await Update(1);
                if (interrupt())
                    return false;
            }

            return true;
        }

        public async ValueTask<(bool interrupted, double elapsedMs)> InterruptTask(int durationMs, Func<bool> interrupt)
        {
            DateTime start = DateTime.Now;
            double elapsedMs;
            while ((elapsedMs = (DateTime.Now - start).TotalMilliseconds) < durationMs)
            {
                await Update(1);
                if (interrupt())
                    return (false, elapsedMs);
            }

            return (true, elapsedMs);
        }

        public async ValueTask<(bool interrupted, double elapsedMs)> InterruptTask(int durationMs, Func<bool> interrupt, Action repeat)
        {
            DateTime start = DateTime.Now;
            double elapsedMs;
            while ((elapsedMs = (DateTime.Now - start).TotalMilliseconds) < durationMs)
            {
                repeat();
                await Update(1);
                if (interrupt())
                    return (false, elapsedMs);
            }

            return (true, elapsedMs);
        }

        public async ValueTask<bool> Interrupt(int durationMs, ValueTask<bool> exit)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < durationMs)
            {
                await Update(1);
                if (await exit)
                    return false;
            }

            return true;
        }

        public async ValueTask While(Func<bool> condition)
        {
            while (condition())
            {
                await Update(1);
            }
        }
    }
}
