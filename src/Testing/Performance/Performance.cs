using System;
using System.Collections.Generic;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Performance
{
    [Trait("Category", "Performance")]
    public class TestPerformance : TestBase
    {
        private readonly Perf _perf;

        public TestPerformance(ITestOutputHelper output) : base(output)
        {
            _perf = new Perf(Write);
        }

        public interface IFoo
        {
        }

        public class Foo : IFoo
        {
            public Foo()
            {
                //Thread.Sleep(1);
                //Task.Delay(1).Wait();
                for (var x = 0; x < 1e3; x++) {}
            }
        }

        [Fact]
        public void Test_Performance()
        {
            var container = new Container().RegisterInstance<IFoo>(new Foo());
            Action action = () => container.Resolve<IFoo>();
            _perf.MeasureRate(action, "resolutions / second from RegisterInstance.");

            container = new Container().RegisterSingleton<IFoo, Foo>();
            action = () => container.Resolve<IFoo>();
            _perf.MeasureRate(action, "resolutions / second from RegisterSingleton.");

            container = new Container().RegisterSingleton<IFoo, Foo>();
            action = () => container.Resolve<IEnumerable<IFoo>>();
            _perf.MeasureRate(action, "enumerable resolutions / second from RegisterSingleton.");

            container = new Container().RegisterTransient<IFoo, Foo>();
            action = () => container.Resolve<IFoo>();
            _perf.MeasureRate(action, "resolutions / second from RegisterTransient.");

            container = new Container().RegisterFactory<IFoo>(() => new Foo());
            action = () => container.Resolve<IFoo>();
            _perf.MeasureRate(action, "resolutions / second from RegisterFactory.");

            container = new Container().RegisterTransient<IFoo, Foo>();
            action = () => container.Resolve<IEnumerable<IFoo>>();
            _perf.MeasureRate(action, "enumerable resolutions / second from RegisterTransient.");

            /*
            container = new Container().RegisterTransient<IFoo,Foo>();
            action = () => container.Resolve<Func<IFoo>>();
            _perf.MeasureRate(action, "factories / second from RegisterTransient.");

            container = new Container().RegisterFactory<IFoo>(() => new Foo());
            action = () => container.Resolve<Func<IFoo>>();
            _perf.MeasureRate(action, "factories / second from RegisterFactory.");
            */

            container = new Container().RegisterFactory<IFoo>(() => new Foo());
            action = () => container.Resolve<Func<IFoo>>()();
            _perf.MeasureRate(action, "factory resolutions / second from RegisterFactory.");

            container = new Container().RegisterTransient<IFoo, Foo>();
            action = () => container.Resolve<Func<IFoo>>()();
            _perf.MeasureRate(action, "factory resolutions / second from RegisterTransient.");

            /*
            container = new Container().RegisterSingleton<IFoo, Foo>();
            action = () => container.Resolve<Func<IEnumerable<IFoo>>>();
            _perf.MeasureRate(action, "factory enumerables / second from RegisterSingleton.");
            */

            container = new Container().RegisterSingleton<IFoo>();
            action = () => container.Resolve<Func<IEnumerable<IFoo>>>()();
            _perf.MeasureRate(action, "factory enumerable resolutions / second from RegisterSingleton.");


            container = new Container().RegisterTransient<IFoo, Foo>();
            action = () => container.Resolve<Func<IEnumerable<IFoo>>>()();
            _perf.MeasureRate(action, "factory enumerable resolutions / second from RegisterTransient.");
        }

    }
}
