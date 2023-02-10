using System;
using System.Diagnostics;

namespace MinimalContainer.Tests.Utility
{
    public class Perf
    {
        private readonly Action<string> Log;
        public Perf(Action<string> log) => Log = log;

        private static double MeasureTicks(Action action)
        {
            action(); // prime
            var counter = 1L;
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                action();
                counter++;
            } while (sw.ElapsedMilliseconds < 100);
            sw.Stop();
            return sw.ElapsedTicks / (double)counter;
        }

        public void MeasureRate(Action action, string label)
        {
            var frequency = Stopwatch.Frequency / MeasureTicks(action);
            Log($"{frequency,12:##,###,###} {label}");
        }

        public void MeasureDuration(Action action, long iterations, string label)
        {
            var ticks = (long)(MeasureTicks(action) * iterations);
            var ts = TimeSpan.FromTicks(ticks);
            Log($"{ts} {label}");
        }
    }
}
