using System;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.TypeFactory
{
    public class TypeFactoryGetInstanceTest : TestBase
    {
        public TypeFactoryGetInstanceTest(ITestOutputHelper output) : base(output) {}

        public class SomeClass {}

        [Fact]
        public void T00_not_registered()
        {
            var container = new Container();
            Assert.Throws<ArgumentException>(() => container.RegisterTransient<Func<SomeClass>>()).Output(Write);
        }

        [Fact]
        public void T01_transient_factory()
        {
            var container = new Container();
            container.RegisterTransient<SomeClass>();
            var factory = container.Resolve<Func<SomeClass>>();
            Assert.IsType(typeof(SomeClass), factory());
            Assert.NotEqual(factory(), factory());
        }

        [Fact]
        public void T02_singleton_factory()
        {
            var container = new Container();
            container.RegisterTransient<SomeClass>();
            container.Resolve<Func<SomeClass>>();
        }

        [Fact]
        public void T03_factory_of_singletons()
        {
            var container = new Container();
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<SomeClass>>()).Output(Write);
        }

        [Fact]
        public void T04_auto_singleton()
        {
            var container = new Container(defaultLifestyle:DefaultLifestyle.Singleton, log:Write);
            container.Resolve<Func<SomeClass>>();
            container.Log();
        }

        [Fact]
        public void T05_auto_transient()
        {
            var container = new Container(DefaultLifestyle.Transient);
            container.Resolve<Func<SomeClass>>();
        }

        [Fact]
        public void T06_factory_from_factory()
        {
            var container = new Container();
            container.RegisterFactory(() => new SomeClass());
            var factory = container.Resolve<Func<SomeClass>>();
            Assert.NotEqual(factory(), factory());
        }

    }
}
