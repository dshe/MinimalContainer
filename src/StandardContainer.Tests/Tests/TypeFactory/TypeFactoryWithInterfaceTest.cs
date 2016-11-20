using System;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.TypeFactory
{
    public class TypeFactoryWithInterfaceTest : TestBase
    {
        public TypeFactoryWithInterfaceTest(ITestOutputHelper output) : base(output) {}

        public interface ISomeClass { }
        public interface ISomeClass2 { }
        public class SomeClass : ISomeClass { }
        public class SomeClass2 : ISomeClass2
        {
            public SomeClass2(Func<ISomeClass> factory) { }
        }

        [Fact]
        public void T01_transient_factory()
        {
            var container = new Container();
            container.RegisterTransient<ISomeClass, SomeClass>();
            var factory = container.Resolve<Func<ISomeClass>>();
            Assert.IsType(typeof(SomeClass), factory());
            Assert.NotEqual(factory(), factory());
        }

        [Fact]
        public void T02_singleton_factory()
        {
            var container = new Container();
            container.RegisterTransient<ISomeClass>();
            container.Resolve<Func<ISomeClass>>();
        }

        [Fact]
        public void T03_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.Resolve<Func<ISomeClass>>();
        }

        [Fact]
        public void T04_auto_singleton_injection()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.Resolve<ISomeClass2>();
        }

    }
}
