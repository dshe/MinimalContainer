using MinimalContainer.Tests.Utility;
using System;
using Xunit;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Relative
{
    public class MultipleSingletonInterfaces : UnitTestBase
    {
        public interface IFoo1 { }
        public interface IFoo2 { }

        public class Foo : IFoo1, IFoo2 { }

        public MultipleSingletonInterfaces(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test_Multiple()
        {
            var container = new Container(logger: Logger);
            container.RegisterSingleton<IFoo1>();
            container.RegisterSingleton<IFoo2>();
            Assert.NotEqual((Foo)container.Resolve<IFoo1>(), (Foo)container.Resolve<IFoo2>());
        }

        [Fact]
        public void Test_Multiple2()
        {
            var container = new Container(logger: Logger);
            container.RegisterSingleton<Foo>();
            container.RegisterSingleton<IFoo1>();
            Assert.NotEqual(container.Resolve<Foo>(), container.Resolve<IFoo1>());
        }
    }
}
