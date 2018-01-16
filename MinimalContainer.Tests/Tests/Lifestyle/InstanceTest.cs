using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Lifestyle
{
    public class InstanceTest
    {
        public interface IFoo { }
        public class Foo : IFoo { }

        private readonly Action<string> Write;
        public InstanceTest(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void T01_Concrete()
        {
            var container = new Container(logAction: Write);
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
            var container = new Container(logAction: Write);
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
