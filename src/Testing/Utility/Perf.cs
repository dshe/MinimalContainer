using System;
using System.Diagnostics;

namespace Testing.Utility
{
    public class Perf
    {
        private readonly Action<string> _write;
        public Perf(Action<string> write)
        {
            _write = write;
        }

        private static double MeasureTicks(Action action)
        {
            action(); // primer
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
            _write($"{frequency,12:##,###,###} {label}");
        }

        public void MeasureDuration(Action action, long iterations, string label)
        {
            var ticks = (long)(MeasureTicks(action) * iterations);
            var ts = TimeSpan.FromTicks(ticks);
            _write($"{ts} {label}");
        }

    }
}
