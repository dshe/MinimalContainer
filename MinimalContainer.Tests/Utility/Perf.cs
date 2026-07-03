using System.Diagnostics;
namespace MinimalContainer.Tests;

public class Perf
{
    private readonly Action<string> _log;
    public Perf(Action<string> log) => _log = log;

    private static double MeasureTicks(Action action)
    {
        action(); // prime
        long counter = 1L;
        Stopwatch sw = new();
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
        double frequency = Stopwatch.Frequency / MeasureTicks(action);
        _log($"{frequency,12:##,###,###} {label}");
    }

    public void MeasureDuration(Action action, long iterations, string label)
    {
        long ticks = (long)(MeasureTicks(action) * iterations);
        TimeSpan ts = TimeSpan.FromTicks(ticks);
        _log($"{ts} {label}");
    }
}
