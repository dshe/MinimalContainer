using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.TypeFactory
{
    public class TypeFactoryInjectionTest : TestBase
    {
        public TypeFactoryInjectionTest(ITestOutputHelper output) : base(output) {}

        public class Foo {}
        public class Bar
        {
            public readonly Func<Foo> Factory;
            public Bar(Func<Foo> factory) 
            {
                Factory = factory;
            }
        }

        [Fact]
        public void T00_injection()
        {
            var container = new Container();
            container.RegisterTransient<Bar>();
            container.RegisterTransient<Foo>();

            var instance = container.Resolve<Bar>();
            Assert.NotEqual(instance.Factory(), instance.Factory());
        }

        [Fact]
        public void T01_auto_singleton_injection()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            //var container = new Container(DefaultLifestyle.Transient);
            //container.RegisterTransient<Bar>();
            //container.RegisterTransient<Foo>();

            var instance = container.Resolve<Bar>();
            Assert.NotEqual(instance.Factory(), instance.Factory());
        }

        [Fact]
        public void T02_auto_transient_injection()
        {
            var container = new Container(DefaultLifestyle.Transient);
            var instance = container.Resolve<Bar>();
            Assert.NotEqual(instance.Factory(), instance.Factory());
        }

    }
}
