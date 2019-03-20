using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
/*
 1.0
 */
#nullable enable

namespace MinimalContainer.Tests.Utility
{
    public class Perf
    {
        private readonly ILogger Logger;
        public Perf(ILogger logger) => Logger = logger;

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
            Logger.LogDebug($"{frequency,12:##,###,###} {label}");
        }

        public void MeasureDuration(Action action, long iterations, string label)
        {
            var ticks = (long)(MeasureTicks(action) * iterations);
            var ts = TimeSpan.FromTicks(ticks);
            Logger.LogDebug($"{ts} {label}");
        }
    }
}
