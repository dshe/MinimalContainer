using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Utility
{
    public class TestBase
    {
        protected readonly Action<string> Write;
        public TestBase(ITestOutputHelper output)
        {
            Write = output.WriteLine;
        }

        private static double MeasureTicks(Action action)
        {
            var counter = 1L;
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                action();
                counter++;
            } while (sw.ElapsedMilliseconds < 300);
            sw.Stop();
            return sw.ElapsedTicks / (double)counter;
        }

        protected void MeasureRate(Action action, string label)
        {
            var frequency = Stopwatch.Frequency / MeasureTicks(action);
            Write($"{frequency,10:####,###} {label}");
        }

        protected void MeasureDuration(Action action, long iterations, string label)
        {
            var ticks = (long)(MeasureTicks(action) * iterations);
            var ts = TimeSpan.FromTicks(ticks);
            Write($"{ts} {label}");
        }

    }
}
