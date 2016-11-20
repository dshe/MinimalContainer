using System;
using System.Linq;
using System.Reflection;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public class MultipleSingletonInterfaces : TestBase
    {
        public MultipleSingletonInterfaces(ITestOutputHelper output) : base(output) {}

        public interface IFoo1 { }
        public interface IFoo2 { }
        public class Foo : IFoo1, IFoo2 { }


        [Fact]
        public void Test_Multiple()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<IFoo1>();
            container.RegisterSingleton<IFoo2>();
            Assert.Equal((Foo)container.Resolve<IFoo1>(), (Foo)container.Resolve<IFoo2>());
        }

        [Fact]
        public void Test_Multiple2()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<Foo>();
            container.RegisterSingleton<IFoo1>();
            Assert.Equal(container.Resolve<Foo>(), container.Resolve<IFoo1>());
        }

    }

}
