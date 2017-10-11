using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.TypeFactory
{
    public class TypeFactoryResolveTest : TestBase
    {
        public TypeFactoryResolveTest(ITestOutputHelper output) : base(output) {}

        public class Foo {}

        [Fact]
        public void T00_not_registered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<Foo>>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T01_factory_from_transient()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<Foo>();
            var factory = container.Resolve<Func<Foo>>();
            Assert.IsType<Foo>(factory());
            Assert.NotEqual(factory(), factory());
        }

        [Fact]
        public void T02_factory_from_factory()
        {
            var container = new Container(log: Write);
            Func<Foo> factory = () => new Foo();
            container.RegisterFactory(factory);
            var f = container.Resolve<Func<Foo>>();
            Assert.Equal(factory.GetType(), f.GetType());
            Assert.NotEqual(f(), f());
        }

        [Fact]
        public void T03_factory_from_singleton()
        {
            var container = new Container(log:Write);
            container.RegisterSingleton<Foo>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<Foo>>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T04_factory_from_instance()
        {
            var container = new Container(log: Write);
            container.RegisterInstance(new Foo());
            Assert.Throws<TypeAccessException>(() => container.Resolve<Func<Foo>>());
        }

        [Fact]
        public void T05_auto_transient()
        {
            var container = new Container(DefaultLifestyle.Transient, Write);
            container.Resolve<Func<Foo>>();
        }

        [Fact]
        public void T06_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            container.Resolve<Func<Foo>>();
        }

        [Fact]
        public void T07_auto_singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            container.Resolve<Func<Foo>>();
        }

        [Fact]
        public void T08_instance_of_factory()
        {
            var container = new Container(log: Write);

            Func<Foo> factory = () => new Foo();
            container.RegisterInstance(factory);

            var f = container.Resolve<Func<Foo>>();
            //Assert.Equal(f, factory);
            Assert.NotEqual(f(), factory());

            Write("");
            container.Log();
        }

    }
}
