using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests
{
    [Trait("Category", "Performance")]
    public class Performance : TestBase
    {
        private readonly Perf perf;
        public Performance(ITestOutputHelper output) : base(output)
        {
            perf = new Perf(output.WriteLine);
        }

        public class ClassA
        {
            public ClassA()
            {
                Task.Delay(1).Wait(0);
            }
        }

        [Fact]
        public void Test_Performance()
        {
            var container = new Container().RegisterInstance(new ClassA());
            Action action = () => container.Resolve<ClassA>();
            perf.MeasureRate(action, "instances from RegisterInstance / second");

            container = new Container().RegisterSingleton<ClassA>();
            action = () => container.Resolve<ClassA>();
            perf.MeasureRate(action, "instances from RegisterSingleton / second");

            container = new Container().RegisterTransient<ClassA>();
            action = () => container.Resolve<ClassA>();
            perf.MeasureRate(action, "instances from RegisterTransient / second");

            container = new Container().RegisterFactory(() => new ClassA());
            action = () => container.Resolve<ClassA>();
            perf.MeasureRate(action, "instances from RegisterFactory / second");

            container = new Container().RegisterTransient<ClassA>();
            action = () => container.Resolve<Func<ClassA>>();
            perf.MeasureRate(action, "factories from RegisterTransient / second");

            container = new Container().RegisterFactory(() => new ClassA());
            action = () => container.Resolve<Func<ClassA>>();
            perf.MeasureRate(action, "factories from RegisterFactory / second");

            container = new Container().RegisterSingleton<ClassA>();
            action = () => container.Resolve<IEnumerable<ClassA>>();
            perf.MeasureRate(action, "enumerable of singleton / second");
        }

    }
}
