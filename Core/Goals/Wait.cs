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
                await Task.Delay(4);
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

        public async ValueTask<Tuple<bool, double>> InterruptTask(int durationMs, Func<bool> interrupt)
        {
            DateTime start = DateTime.Now;
            double elapsedMs;
            while ((elapsedMs = (DateTime.Now - start).TotalMilliseconds) < durationMs)
            {
                await Update(1);
                if (interrupt())
                    return Tuple.Create(false, elapsedMs);
            }

            return Tuple.Create(true, elapsedMs);
        }

        public async ValueTask<Tuple<bool, double>> InterruptTask(int durationMs, Func<bool> interrupt, Action repeat)
        {
            DateTime start = DateTime.Now;
            double elapsedMs;
            while ((elapsedMs = (DateTime.Now - start).TotalMilliseconds) < durationMs)
            {
                repeat();
                await Update(1);
                if (interrupt())
                    return Tuple.Create(false, elapsedMs);
            }

            return Tuple.Create(true, elapsedMs);
        }

        public async ValueTask<bool> Interrupt(int durationMs, Task<bool> exit)
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
