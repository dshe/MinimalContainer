using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Performance
{
    public class Performance
    {
        private readonly Container container = new Container();

        private readonly Stopwatch sw = new Stopwatch();
        private readonly ITestOutputHelper output;
        public Performance(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test_Performance1()
        {
            var types = typeof(string).Assembly.GetTypes().Where(t => !t.IsAbstract).ToList();

            sw.Start();
            RegisterTypes(types, Lifestyle.Transient);
            sw.Stop();
            var rate = types.Count / sw.Elapsed.TotalSeconds;
            output.WriteLine($"{rate,10:####,###} registrations/second ({types.Count} types).");

            sw.Restart();
            foreach (var type in types)
                container.GetInstance(type);
            sw.Stop();
            rate = types.Count / sw.Elapsed.TotalSeconds;
            output.WriteLine($"{rate,10:####,###} transient instances/second ({types.Count} types).");

            sw.Restart();
            foreach (var type in types)
                container.GetInstance(type);
            sw.Stop();
            rate = types.Count / sw.Elapsed.TotalSeconds;
            output.WriteLine($"{rate,10:####,###} transient instances/second ({types.Count} types).");
            container.Dispose();


            RegisterTypes(types, Lifestyle.Singleton);
            sw.Restart();
            foreach (var type in types)
                container.GetInstance(type);
            sw.Stop();
            rate = types.Count / sw.Elapsed.TotalSeconds;
            output.WriteLine($"{rate,10:####,###} singleton instances/second ({types.Count} types).");

            sw.Restart();
            foreach (var type in types)
                container.GetInstance(type);
            sw.Stop();
            rate = types.Count / sw.Elapsed.TotalSeconds;
            output.WriteLine($"{rate,10:####,###} singleton instances/second ({types.Count} types).");
            container.Dispose();

            Assert.True(true);
        }

        internal void RegisterTypes(List<Type> types, Lifestyle lifestyle)
        {
            foreach (var type in types)
                container.RegisterFactory(type, () => type);
        }

        public class ClassA { }

        [Fact]
        public void Test_Performance2()
        {
            var iterations = 1e6;

            container.RegisterTransient<ClassA>();
            sw.Start();
            for (var i = 0; i < iterations; i++)
            {
                var x = container.GetInstance<ClassA>();
            }
            sw.Stop();
            var rate = iterations / sw.Elapsed.TotalSeconds;
            output.WriteLine($"{rate,10:####,###} instances/second.");
            container.Dispose();

            container.RegisterSingleton<ClassA>();
            sw.Restart();
            for (var i = 0; i < iterations; i++)
            {
                var x = container.GetInstance<ClassA>();
            }
            sw.Stop();
            rate = iterations / sw.Elapsed.TotalSeconds;
            output.WriteLine($"{rate,10:####,###} instances/second.");

            Assert.True(true);
        }


    }
}
