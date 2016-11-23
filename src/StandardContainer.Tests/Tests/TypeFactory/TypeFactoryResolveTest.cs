using System;
using StandardContainer.Tests.Tests.Other;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.TypeFactory
{
    public class TypeFactoryResolveTest : TestBase
    {
        public TypeFactoryResolveTest(ITestOutputHelper output) : base(output) {}

        public class SomeClass {}

        [Fact]
        public void T00_not_registered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<SomeClass>>()).Output(Write);
        }

        [Fact]
        public void T01_factory_from_transient()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<SomeClass>();
            var factory = container.Resolve<Func<SomeClass>>();
            Assert.IsType(typeof(SomeClass), factory());
            Assert.NotEqual(factory(), factory());
        }

        [Fact]
        public void T02_factory_from_factory()
        {
            var container = new Container(log: Write);
            Func<SomeClass> factory = () => new SomeClass();
            container.RegisterFactory(factory);
            var f = container.Resolve<Func<SomeClass>>();
            Assert.Equal(factory, f);
            Assert.NotEqual(f(), f());
        }

        [Fact]
        public void T03_factory_from_singleton()
        {
            var container = new Container(log:Write);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<SomeClass>>()).Output(Write);
        }

        [Fact]
        public void T04_factory_from_instance()
        {
            var container = new Container(log: Write);
            container.RegisterInstance(new SomeClass());
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<SomeClass>>()).Output(Write);
        }

        [Fact]
        public void T05_auto_transient()
        {
            var container = new Container(DefaultLifestyle.Transient, log: Write);
            container.Resolve<Func<SomeClass>>();
        }

        [Fact]
        public void T06_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, log:Write);
            // SomeClass is registered as transient
            container.Resolve<Func<SomeClass>>();

            Write("");
            container.Log();
        }

        [Fact]
        public void T07_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);

            // SomeClass registered as singleton
            container.Resolve<SomeClass>();

            // cannot resolve Func of singleton
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<SomeClass>>()).Output(Write);

            Write("");
            container.Log();
        }

        [Fact]
        public void T08_instance_of_factory()
        {
            var container = new Container(log: Write);

            Func<SomeClass> factory = () => new SomeClass();
            container.RegisterInstance(factory);

            var f = container.Resolve<Func<SomeClass>>();
            Assert.Equal(f, factory);
            Assert.NotEqual(f(), factory());

            Write("");
            container.Log();
        }

    }
}
