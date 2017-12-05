using System;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.TypeFactory
{
    public class TypeFactoryWithInterfaceTest
    {
        public interface IFoo { }
        public interface IBar { }

        public class Foo : IFoo { }
        public class Bar : IBar
        {
            public Bar(Func<IFoo> factory) { }
        }

        private readonly Action<string> Write;
        public TypeFactoryWithInterfaceTest(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void T01_transient_factory()
        {
            var container = new Container();
            container.RegisterTransient<IFoo, Foo>();
            var factory = container.Resolve<Func<IFoo>>();
            Assert.IsType<Foo>(factory());
            Assert.NotEqual(factory(), factory());
        }

        [Fact]
        public void T02_singleton_factory()
        {
            var container = new Container();
            container.RegisterTransient<IFoo>();
            container.Resolve<Func<IFoo>>();
        }

        [Fact]
        public void T03_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.Resolve<Func<IFoo>>();
        }

        [Fact]
        public void T04_auto_singleton_injection()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.Resolve<IBar>();
        }
    }
}
