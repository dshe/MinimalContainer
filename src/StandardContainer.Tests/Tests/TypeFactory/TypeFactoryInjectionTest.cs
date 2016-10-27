using System;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.TypeFactory
{
    public class TypeFactoryInjectionTest
    {
        private readonly Action<string> write;
        public TypeFactoryInjectionTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        public class SomeClass {}
        public class SomeClass2
        {
            public readonly Func<SomeClass> Factory;
            public SomeClass2(Func<SomeClass> factory)
            {
                Factory = factory;
            }
        }

        [Fact]
        public void T00_injection()
        {
            var container = new Container();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterTransient<SomeClass>();
            var instance = container.GetInstance<SomeClass2>();
            Assert.NotEqual(instance.Factory(), instance.Factory());
        }

        [Fact]
        public void T01_auto_singleton_injection()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            var instance = container.GetInstance<SomeClass2>();
            Assert.NotEqual(instance.Factory(), instance.Factory());
        }
        [Fact]
        public void T02_auto_transient_injection()
        {
            var container = new Container(DefaultLifestyle.Transient);
            var instance = container.GetInstance<SomeClass2>();
            Assert.NotEqual(instance.Factory(), instance.Factory());
        }

    }
}
