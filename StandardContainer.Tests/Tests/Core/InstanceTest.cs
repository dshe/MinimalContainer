using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class InstanceTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Container container;
        private readonly Action<string> write;

        public InstanceTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(log: write);
        }

        [Fact]
        public void T01_Concrete()
        {
            var instance = new SomeClass();
            container.RegisterInstance(instance);
            var instance1 = container.GetInstance<SomeClass>();
            Assert.Equal(instance, instance1);
            var instance2 = container.GetInstance<SomeClass>();
            Assert.Equal(instance1, instance2);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ISomeClass>()).Output(write);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(instance)).Output(write);
        }

        [Fact]
        public void T02_Interface()
        {
            var instance = new SomeClass();
            container.RegisterInstance<ISomeClass>(instance);
            var instance1 = container.GetInstance<ISomeClass>();
            Assert.Equal(instance, instance1);
            var instance2 = container.GetInstance<ISomeClass>();
            Assert.Equal(instance1, instance2);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<SomeClass>());
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance<ISomeClass>(instance)).Output(write);
        }

    }
}
