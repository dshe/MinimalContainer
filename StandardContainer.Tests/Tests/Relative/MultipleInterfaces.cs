using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public interface IFoo1 {}
    public interface IFoo2 {}

    public class Foo : IFoo1, IFoo2 {}

    public class MultipleInterfaces
    {
        private readonly Container container;

        public MultipleInterfaces(ITestOutputHelper output)
        {
            container = new Container(Container.DefaultLifestyle.AutoRegisterDisabled, log: output.WriteLine, assemblies: Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Multiple()
        {
            container.RegisterSingleton<IFoo1>();
            container.RegisterSingleton<IFoo2>();
            Assert.Equal((Foo)container.GetInstance<IFoo1>(), (Foo)container.GetInstance<IFoo2>()); // why is cast required?
        }

        [Fact]
        public void Test_Multiple2()
        {
            container.RegisterSingleton<Foo>();
            container.RegisterSingleton<IFoo1>();
            Assert.Equal(container.GetInstance<Foo>(), container.GetInstance<IFoo1>());
        }

    }

}
