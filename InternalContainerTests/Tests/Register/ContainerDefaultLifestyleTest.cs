using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InternalContainer;
using InternalContainerTests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainerTests.Tests.Register
{
    public class ContainerDefaultLifestyleTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Action<string> write;
        public ContainerDefaultLifestyleTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void Test_Unregistered()
        {
            var c = new Container(log: write);
            Assert.Throws<TypeAccessException>(() => c.GetInstance<SomeClass>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<IEnumerable<ISomeClass>>()).Output(write);
        }

        [Fact]
        public void Test_Singleton()
        {
            var c = new Container(Container.Lifestyle.Singleton, log: write, assemblies:Assembly.GetExecutingAssembly());
            var instance1 = c.GetInstance<SomeClass>();
            var reg = c.GetRegistrations().Last();
            Assert.Equal(Container.Lifestyle.Singleton, reg.Lifestyle);
            var instance2 = c.GetInstance<SomeClass>();
            Assert.Equal(instance1, instance2);
        }

        [Fact]
        public void Test_Transient()
        {
            var c = new Container(Container.Lifestyle.Transient, log: write, assemblies: Assembly.GetExecutingAssembly());
            var instance1 = c.GetInstance<SomeClass>();
            var reg = c.GetRegistrations().Last();
            Assert.Equal(Container.Lifestyle.Transient, reg.Lifestyle);
            var instance2 = c.GetInstance<SomeClass>();
            Assert.NotEqual(instance1, instance2);
        }
    }
}
