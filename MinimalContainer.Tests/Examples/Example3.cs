using System;
using Xunit;
using MinimalContainer;

namespace MinimalContainerTests
{
    internal interface IBar { }
    internal class Bar : IBar { }

    internal interface IFoo
    {
        Func<IBar> BarFactory { get; }
    }

    internal class Foo : IFoo
    {
        public Func<IBar> BarFactory { get; }
        internal Foo(Func<IBar> barFactory) => BarFactory = barFactory;
    }

    internal class Root
    {
        private readonly IFoo _foo;
        internal Root(IFoo foo) => _foo = foo;

        private void StartApplication()
        {
            var bar = _foo.BarFactory();
            Assert.IsType<Bar>(bar);
        }

        public static void Mainx()
        {
            new Container(DefaultLifestyle.Transient)
                .Resolve<Root>()
                .StartApplication();
        }
    }
}
