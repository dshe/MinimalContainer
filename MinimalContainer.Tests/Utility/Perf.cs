using System;
using System.Diagnostics;
/*
 1.0
 */

namespace MinimalContainer.Tests.Utility
{
    public class Perf
    {
        private readonly Action<string> write;
        public Perf(Action<string> write) => this.write = write;

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
            write($"{frequency,12:##,###,###} {label}");
        }

        public void MeasureDuration(Action action, long iterations, string label)
        {
            var ticks = (long)(MeasureTicks(action) * iterations);
            var ts = TimeSpan.FromTicks(ticks);
            write($"{ts} {label}");
        }
    }
}
