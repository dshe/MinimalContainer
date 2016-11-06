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
        private const long Iterations = (long)1e6;

        [Fact]
        public void Test_Performance()
        {
            var container = new Container();
            container.RegisterInstance(new ClassA());
            sw.Restart();
            for (var i = 0; i < Iterations; i++)
                container.Resolve<ClassA>();
            sw.Stop();
            WriteResult("instances/second");

            container = new Container();
            container.RegisterSingleton<ClassA>();
            container.Resolve<ClassA>();
            sw.Restart();
            for (var i = 0; i < Iterations; i++)
                container.Resolve<ClassA>();
            sw.Stop();
            WriteResult("singletons/second");

            container = new Container();
            container.RegisterTransient<ClassA>();
            sw.Restart();
            for (var i = 0; i < Iterations; i++)
                container.Resolve<ClassA>();
            sw.Stop();
            WriteResult("transients/second");

            container = new Container();
            var instance = new ClassA();
            container.RegisterFactory(() => instance);
            sw.Restart();
            for (var i = 0; i < Iterations; i++)
                container.Resolve<ClassA>();
            sw.Stop();
            WriteResult("factory instances/second");

            Assert.True(true);
        }

        private void WriteResult(string label)
        {
            var rate = Iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} {label}.");
        }

    }
}
