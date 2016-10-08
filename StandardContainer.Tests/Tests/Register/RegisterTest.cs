using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Register
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
            Assert.Equal(Style.Singleton, reg.Style);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(null, reg.Factory);
            container.Dispose();

            container.RegisterSingleton(typeof(ISomeClass), typeof(SomeClass));
            reg = container.GetRegistrations().Last();
            Assert.Equal(Style.Singleton, reg.Style);
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(null, reg.Factory);
        }

        [Fact]
        public void T02_Register_Transient()
        {
            container.RegisterTransient<ISomeClass, SomeClass>();
            var reg = container.GetRegistrations().Last();
            Assert.Equal(Style.Transient, reg.Style);
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(null, reg.Factory);
        }

        [Fact]
        public void T03_Register_Instance()
        {
            var instance = new SomeClass();

            container.RegisterInstance(instance);
            var reg = container.GetRegistrations().Last();
            Assert.Equal(Style.Instance, reg.Style);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(instance, reg.Instance);
            Assert.Equal(null, reg.Factory);
            container.Dispose();

            container.RegisterInstance<ISomeClass>(instance);
            reg = container.GetRegistrations().Last();
            Assert.Equal(Style.Instance, reg.Style);
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(instance, reg.Instance);
            Assert.Equal(null, reg.Factory);
        }

        [Fact]
        public void T04_Register_Factory()
        {
            Func<SomeClass> factory = () => new SomeClass();

            container.RegisterFactory(factory);
            var reg = container.GetRegistrations().Last();
            Assert.Equal(Style.Factory, reg.Style);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(null, reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(factory, reg.Factory); 
            container.Dispose();

            container.RegisterFactory<ISomeClass>(factory);
            reg = container.GetRegistrations().Last();
            Assert.Equal(Style.Factory, reg.Style);
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.Type);
            Assert.Equal(null, reg.ConcreteType);
            Assert.Equal(null, reg.Instance);
            Assert.Equal(factory, reg.Factory);
        }
    }
}
