using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Lifestyle
{
    public class InstanceTest : TestBase
    {
        public InstanceTest(ITestOutputHelper output) : base(output) {}

        public interface IFoo { }
        public class Foo : IFoo { }

        [Fact]
        public void T01_Concrete()
        {
            var container = new Container(log: Write);
            var instance = new Foo();
            container.RegisterInstance(instance);
            var instance1 = container.Resolve<Foo>();
            Assert.Equal(instance, instance1);
            var instance2 = container.Resolve<Foo>();
            Assert.Equal(instance1, instance2);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IFoo>()).WriteMessageTo(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(instance)).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Interface()
        {
            var container = new Container(log: Write);
            var instance = new Foo();
            container.RegisterInstance<IFoo>(instance);
            var instance1 = container.Resolve<IFoo>();
            Assert.Equal(instance, instance1);
            var instance2 = container.Resolve<IFoo>();
            Assert.Equal(instance1, instance2);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>());
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance<IFoo>(instance)).WriteMessageTo(Write);
        }

    }
}
