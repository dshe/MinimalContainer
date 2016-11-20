using System;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Lifestyle
{
    public class InstanceTest : TestBase
    {
        public InstanceTest(ITestOutputHelper output) : base(output) {}

        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        [Fact]
        public void T01_Concrete()
        {
            var container = new Container(log: Write);
            var instance = new SomeClass();
            container.RegisterInstance(instance);
            var instance1 = container.Resolve<SomeClass>();
            Assert.Equal(instance, instance1);
            var instance2 = container.Resolve<SomeClass>();
            Assert.Equal(instance1, instance2);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>()).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(instance)).Output(Write);
        }

        [Fact]
        public void T02_Interface()
        {
            var container = new Container(log: Write);
            var instance = new SomeClass();
            container.RegisterInstance<ISomeClass>(instance);
            var instance1 = container.Resolve<ISomeClass>();
            Assert.Equal(instance, instance1);
            var instance2 = container.Resolve<ISomeClass>();
            Assert.Equal(instance1, instance2);
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>());
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance<ISomeClass>(instance)).Output(Write);
        }

    }
}
