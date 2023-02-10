using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Performance
{
    [Trait("Category", "Performance")]
    public class TestPerformance : BaseUnitTest
    {
        public interface IFoo { }

        public class Foo : IFoo
        {
            public Foo() { }
        }

        private readonly Perf perf;

        public TestPerformance(ITestOutputHelper output) : base(output)
            => perf = new Perf(Log);

        [Fact]
        public void Test_Performance()
        {
            var container = new Container().RegisterInstance<IFoo>(new Foo());
            Action action = () => container.Resolve<IFoo>();
            perf.MeasureRate(action, "resolutions / second from RegisterInstance.");

            container = new Container().RegisterSingleton<IFoo, Foo>();
            action = () => container.Resolve<IFoo>();
            perf.MeasureRate(action, "resolutions / second from RegisterSingleton.");

            container = new Container().RegisterSingleton<IFoo, Foo>();
            action = () => container.Resolve<IEnumerable<IFoo>>();
            perf.MeasureRate(action, "enumerable resolutions / second from RegisterSingleton.");

            container = new Container().RegisterTransient<IFoo, Foo>();
            action = () => container.Resolve<IFoo>();
            perf.MeasureRate(action, "resolutions / second from RegisterTransient.");

            container = new Container().RegisterFactory<IFoo>(() => new Foo());
            action = () => container.Resolve<IFoo>();
            perf.MeasureRate(action, "resolutions / second from RegisterFactory.");

            container = new Container().RegisterTransient<IFoo, Foo>();
            action = () => container.Resolve<IEnumerable<IFoo>>();
            perf.MeasureRate(action, "enumerable resolutions / second from RegisterTransient.");

            /*
            container = new Container().RegisterTransient<IFoo,Foo>();
            action = () => container.Resolve<Func<IFoo>>();
            perf.MeasureRate(action, "factories / second from RegisterTransient.");

            container = new Container().RegisterFactory<IFoo>(() => new Foo());
            action = () => container.Resolve<Func<IFoo>>();
            perf.MeasureRate(action, "factories / second from RegisterFactory.");
            */

            container = new Container().RegisterFactory<IFoo>(() => new Foo());
            action = () => container.Resolve<Func<IFoo>>()();
            perf.MeasureRate(action, "factory resolutions / second from RegisterFactory.");

            container = new Container().RegisterTransient<IFoo, Foo>();
            action = () => container.Resolve<Func<IFoo>>()();
            perf.MeasureRate(action, "factory resolutions / second from RegisterTransient.");

            /*
            container = new Container().RegisterSingleton<IFoo, Foo>();
            action = () => container.Resolve<Func<IEnumerable<IFoo>>>();
            perf.MeasureRate(action, "factory enumerables / second from RegisterSingleton.");
            */

            container = new Container().RegisterSingleton<IFoo>();
            action = () => container.Resolve<Func<IEnumerable<IFoo>>>()();
            perf.MeasureRate(action, "factory enumerable resolutions / second from RegisterSingleton.");


            container = new Container().RegisterTransient<IFoo, Foo>();
            action = () => container.Resolve<Func<IEnumerable<IFoo>>>()();
            perf.MeasureRate(action, "factory enumerable resolutions / second from RegisterTransient.");
        }

    }
}
