using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class GetInstanceTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Container container;
        private readonly Action<string> write;

        public GetInstanceTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(log: write, assemblies: Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void T01_GetInstance_Singleton()
        {
            container.RegisterSingleton<SomeClass>();
            var reg = container.GetRegistrations().Last();
            Assert.Equal(0, reg.Count);
            var instance1 = container.GetInstance<SomeClass>();
            Assert.Equal(1, reg.Count);
            var instance2 = container.GetInstance<SomeClass>();
            Assert.Equal(1, reg.Count);
            Assert.Equal(instance2, instance1);
        }

        [Fact]
        public void T02_GetInstance_Transient()
        {
            container.RegisterTransient<SomeClass>();
            var reg = container.GetRegistrations().Last();
            Assert.Equal(0, reg.Count);
            var instance1 = container.GetInstance<SomeClass>();
            Assert.Equal(1, reg.Count);
            var instance2 = container.GetInstance<SomeClass>();
            Assert.Equal(2, reg.Count);
            Assert.NotEqual(instance2, instance1);
        }

        [Fact]
        public void T03_GetInstance_Instance()
        {
            var instance = new SomeClass();
            container.RegisterInstance(instance);
            var reg = container.GetRegistrations().Last();
            Assert.Equal(1, reg.Count);
            var instance1 = container.GetInstance<SomeClass>();
            Assert.Equal(1, reg.Count);
            Assert.Equal(instance, instance1);
        }

        [Fact]
        public void T04_GetInstance_Factory()
        {
            Func<SomeClass> factory = () => new SomeClass();
            container.RegisterFactory(factory);
            var reg = container.GetRegistrations().Last();
            Assert.Equal(0, reg.Count);
            var instance1 = container.GetInstance<SomeClass>();
            Assert.Equal(1, reg.Count);
            var instance2 = container.GetInstance<SomeClass>();
            Assert.Equal(2, reg.Count);
            Assert.NotEqual(instance2, instance1);
        }
    }
}
