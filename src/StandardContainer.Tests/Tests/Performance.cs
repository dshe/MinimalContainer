using System;
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
        public Performance(ITestOutputHelper output) : base(output) {}

        public class ClassA {}

        [Fact]
        public void Test_Performance()
        {
            var container = new Container().RegisterInstance(new ClassA());
            Action action = () => container.Resolve<ClassA>();
            MeasureRate(action, "instances/second");

            container = new Container().RegisterSingleton<ClassA>();
            action = () => container.Resolve<ClassA>();
            MeasureRate(action, "singletons/second");

            container = new Container().RegisterTransient<ClassA>();
            action = () => container.Resolve<ClassA>();
            MeasureRate(action, "transients/second");

            container = new Container().RegisterFactory(() => new ClassA());
            action = () => container.Resolve<ClassA>();
            MeasureRate(action, "factory instances/second");
        }

    }
}
