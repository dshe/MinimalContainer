using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Performance
{
    public class Performance
    {
        private readonly Stopwatch sw = new Stopwatch();
        private readonly Container container = new Container(log:Console.WriteLine);
        private readonly ITestOutputHelper output;
        public Performance(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test_Performance()
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
            container.Dispose();

            RegisterTypes(types, Lifestyle.Singleton);
            sw.Restart();
            foreach (var type in types)
                container.GetInstance(type);
            sw.Stop();
            rate = types.Count / sw.Elapsed.TotalSeconds;
            output.WriteLine($"{rate,10:####,###} singleton instances/second ({types.Count} types).");

            Assert.True(true);
        }

        internal void RegisterTypes(List<Type> types, Lifestyle lifestyle)
        {
            foreach (var type in types)
                container.Register(type, type, () => type, lifestyle);
        }
    }
}
