using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StandardContainer;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainerTests.Performance
{
    public class Performance
    {
        private readonly Container container = new Container();

        private readonly Stopwatch sw = new Stopwatch();
        private readonly Action<string> write;
        public Performance(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void Test_Performance1()
        {
            var types = typeof(string).Assembly.GetTypes().Where(t => !t.IsAbstract).ToList();

            sw.Start();
            RegisterFactories(types);
            sw.Stop();
            var rate = types.Count / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} registrations/second ({types.Count} types).");

            sw.Restart();
            foreach (var type in types)
                container.GetInstance(type);
            sw.Stop();
            rate = types.Count / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} initial transient instances/second ({types.Count} types).");

            sw.Restart();
            foreach (var type in types)
                container.GetInstance(type);
            sw.Stop();
            rate = types.Count / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} transient instances/second ({types.Count} types).");
            container.Dispose();

            Assert.True(true);
        }

        internal void RegisterFactories(List<Type> types)
            => types.ForEach(type => container.RegisterFactory(type, () => type));

        public class ClassA { }

        [Fact]
        public void Test_Performance2()
        {
            const double iterations = 1e6;

            container.RegisterTransient<ClassA>();
            sw.Start();
            for (var i = 0; i < iterations; i++)
            {
                var x = container.GetInstance<ClassA>();
            }
            sw.Stop();
            var rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} Transient instances/second.");
            container.Dispose();

            container.RegisterSingleton<ClassA>();
            sw.Restart();
            for (var i = 0; i < iterations; i++)
            {
                var x = container.GetInstance<ClassA>();
            }
            sw.Stop();
            rate = iterations / sw.Elapsed.TotalSeconds;
            write($"{rate,10:####,###} Singleton instances/second.");

            Assert.True(true);
        }


    }
}
