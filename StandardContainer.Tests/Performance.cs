using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests
{
    [Trait("Category", "Performance")]
    public class Performance
    {
        private readonly Stopwatch sw = new Stopwatch();
        private readonly Action<string> write;
        public Performance(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        public class ClassA { }

        [Fact]
        public void Test_Performance()
        {
            const long iterations = (long)1e6;

            var container = new Container();
            container.RegisterInstance(new ClassA());
            sw.Restart();
            for (var i = 0; i < iterations; i++)
                container.GetInstance<ClassA>();
            sw.Stop();
            var rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} instances/second.");

            container = new Container();
            container.RegisterSingleton<ClassA>();
            container.GetInstance<ClassA>();
            sw.Restart();
            for (var i = 0; i < iterations; i++)
                container.GetInstance<ClassA>();
            sw.Stop();
            rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} singletons/second.");

            container = new Container();
            container.RegisterTransient<ClassA>();
            sw.Restart();
            for (var i = 0; i < iterations; i++)
                container.GetInstance<ClassA>();
            sw.Stop();
            rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} transients/second.");

            container = new Container();
            var instance = new ClassA();
            container.RegisterFactory(() => instance);
            sw.Restart();
            for (var i = 0; i < iterations; i++)
                container.GetInstance<ClassA>();
            sw.Stop();
            rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} factory instances/second.");

            Assert.True(true);
        }

    }
}
