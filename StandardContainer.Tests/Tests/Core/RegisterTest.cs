using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class RegisterTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Container container;
        private readonly Action<string> write;

        public RegisterTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(log: write, assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void T01_Register_Singleton()
        {
            container.RegisterSingleton(typeof(SomeClass));
            var reg = container.GetRegistrations().Last();
            Assert.Equal(Lifestyle.Singleton, reg.Lifestyle);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(0, reg.Count);
            var instance = container.GetInstance<SomeClass>();
            Assert.Equal(1, reg.Count);




            container.Dispose();

            container.RegisterSingleton(typeof(ISomeClass), typeof(SomeClass));
            reg = container.GetRegistrations().Last();
            Assert.Equal(Lifestyle.Singleton, reg.Lifestyle);
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(0, reg.Count);
        }

        [Fact]
        public void T02_Register_Transient()
        {
            container.RegisterTransient<ISomeClass, SomeClass>();
            var reg = container.GetRegistrations().Last();
            Assert.Equal(Lifestyle.Transient, reg.Lifestyle);
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(0, reg.Count);
        }

        [Fact]
        public void T03_Register_Instance()
        {
            var instance = new SomeClass();

            container.RegisterInstance(instance);
            var reg = container.GetRegistrations().Last();
            Assert.Equal(Lifestyle.Instance, reg.Lifestyle);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(instance, reg.Instance);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(1, reg.Count);
            container.Dispose();

            container.RegisterInstance<ISomeClass>(instance);
            reg = container.GetRegistrations().Last();
            Assert.Equal(Lifestyle.Instance, reg.Lifestyle);
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(instance, reg.Instance);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(1, reg.Count);
        }

        [Fact]
        public void T04_Register_Factory()
        {
            Func<SomeClass> factory = () => new SomeClass();

            container.RegisterFactory(factory);
            var reg = container.GetRegistrations().Last();
            Assert.Equal(Lifestyle.Factory, reg.Lifestyle);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(null, reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(factory, reg.Factory);
            Assert.Equal(0, reg.Count);
            container.Dispose();

            container.RegisterFactory<ISomeClass>(factory);
            reg = container.GetRegistrations().Last();
            Assert.Equal(Lifestyle.Factory, reg.Lifestyle);
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(null, reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(factory, reg.Factory);
            Assert.Equal(0, reg.Count);
        }
    }
}
