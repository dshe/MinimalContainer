using System;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.TypeFactory
{
    public class TypeFactoryWithInterfaceTest
    {
        public interface ISomeClass { }
        public interface ISomeClass2 { }
        public class SomeClass : ISomeClass { }
        public class SomeClass2 : ISomeClass2
        {
            public SomeClass2(Func<ISomeClass> factory) { }
        }

        private readonly Action<string> write;
        public TypeFactoryWithInterfaceTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void T01_transient_factory()
        {
            var container = new Container();
            container.RegisterTransient<ISomeClass, SomeClass>();
            var factory = container.GetInstance<Func<ISomeClass>>();
            Assert.IsType(typeof(SomeClass), factory());
            Assert.NotEqual(factory(), factory());
        }
        [Fact]
        public void T02_singleton_factory()
        {
            var container = new Container();
            container.RegisterTransient<ISomeClass>();
            container.GetInstance<Func<ISomeClass>>();
        }
        [Fact]
        public void T03_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.GetInstance<Func<ISomeClass>>();
        }
        [Fact]
        public void T04_auto_singleton_injection()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            container.GetInstance<ISomeClass2>();
        }

    }
}
