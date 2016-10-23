using System;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.TypeFactory
{
    public class TypeFactoryGetInstanceTest
    {
        public class SomeClass {}

        private readonly Action<string> write;
        public TypeFactoryGetInstanceTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void T00_not_registered()
        {
            var container = new Container();
            //Assert.Throws<TypeAccessException>(() => container.GetInstance<Func<SomeClass>>()).Output(write);
            Assert.Throws<ArgumentException>(() => container.RegisterTransient<Func<SomeClass>>()).Output(write);
        }
        [Fact]
        public void T01_transient_factory()
        {
            var container = new Container();
            container.RegisterTransient<SomeClass>();
            var factory = container.GetInstance<Func<SomeClass>>();
            Assert.IsType(typeof(SomeClass), factory());
            Assert.NotEqual(factory(), factory());
        }
        [Fact]
        public void T02_singleton_factory()
        {
            var container = new Container();
            container.RegisterTransient<SomeClass>();
            container.GetInstance<Func<SomeClass>>();
        }
        [Fact]
        public void T03_factory_of_singletons()
        {
            var container = new Container();
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.GetInstance<Func<SomeClass>>()).Output(write);
        }
        [Fact]
        public void T04_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.GetInstance<Func<SomeClass>>();
        }
        [Fact]
        public void T05_auto_transient()
        {
            var container = new Container(DefaultLifestyle.Transient);
            container.GetInstance<Func<SomeClass>>();
        }

        [Fact]
        public void T06_factory_from_factory()
        {
            var container = new Container();
            container.RegisterFactory(() => new SomeClass());
            var factory = container.GetInstance<Func<SomeClass>>();
            Assert.NotEqual(factory(), factory());
        }

    }
}
