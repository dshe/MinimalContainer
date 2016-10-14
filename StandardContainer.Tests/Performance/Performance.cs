using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Performance
{
    [Trait("Category", "Performance")]
    public class Performance
    {
        public class ClassA { }

        private readonly Stopwatch sw = new Stopwatch();
        private readonly Action<string> write;
        public Performance(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void Test_Performance1()
        {
            const long iterations = (long)1e6;

            var container = new Container();
            container.RegisterInstance(new ClassA());
            sw.Start();
            for (var i = 0; i < iterations; i++)
                container.GetInstance<ClassA>();
            sw.Stop();
            var rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} instances/second.");

            container = new Container();
            container.RegisterSingleton<ClassA>();
            sw.Start();
            for (var i = 0; i < iterations; i++)
                container.GetInstance<ClassA>();
            sw.Stop();
            rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} singleton instances/second.");

            container = new Container();
            container.RegisterTransient<ClassA>();
            sw.Start();
            for (var i = 0; i < iterations; i++)
                container.GetInstance<ClassA>();
            sw.Stop();
            rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} transient instances/second.");

            container = new Container();
            var instance = new ClassA();
            container.RegisterFactory(() => instance);
            sw.Start();
            for (var i = 0; i < iterations; i++)
                container.GetInstance<ClassA>();
            sw.Stop();
            rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} factory instances/second.");

            Assert.True(true);
        }

    }
}
