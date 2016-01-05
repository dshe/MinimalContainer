using System;
using System.Linq;
using System.Reflection;
using InternalContainer.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class RegisterTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }
        private readonly Container container;
        private readonly ITestOutputHelper output;

        public RegisterTest(ITestOutputHelper output)
        {
            this.output = output;
            container = new Container(log: output.WriteLine);
        }

        [Fact]
        public void Test_Register_Singleton()
        {
            container.RegisterSingleton<SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);
            Assert.Equal(false, map.AutoRegistered);
            Assert.Equal(0, map.InstancesCreated);

            var instance = container.GetInstance<SomeClass>();
            Assert.IsType<SomeClass>(instance);

            Assert.Equal(instance, map.Factory());
            Assert.Equal(instance, container.GetInstance(typeof(SomeClass)));
            Assert.Single(container.Maps());
        }

        [Fact]
        public void Test_Register_Singleton_Iface()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);

            var instance = container.GetInstance<ISomeClass>();
            Assert.Equal(instance, map.Factory.Invoke());
            Assert.Equal(container.GetInstance(typeof(ISomeClass)), map.Factory.Invoke());
            Assert.Single(container.Maps());
        }

        [Fact]
        public void Test_Register_Transient()
        {
            container.RegisterTransient<SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);

        }

        [Fact]
        public void Test_Register_Transient_Iface()
        {
            container.RegisterTransient<ISomeClass, SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);

            var instance = container.GetInstance<ISomeClass>();
            Assert.IsType<SomeClass>(instance);
            Assert.NotEqual(instance, container.GetInstance<ISomeClass>());
            Assert.Single(container.Maps());
        }

        [Fact]
        public void Test_Register_Instance()
        {
            var instance = new SomeClass();
            container.RegisterInstance(instance);
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(instance, map.Factory());
            Assert.Equal(instance, map.Factory());
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);

            Assert.Throws<TypeAccessException>(() => container.GetInstance<ISomeClass>());
            Assert.Equal(container.GetInstance<SomeClass>(), map.Factory.Invoke());
            Assert.Equal(container.GetInstance(typeof(SomeClass)), map.Factory.Invoke());
            Assert.Single(container.Maps());
        }

        [Fact]
        public void Test_Register_Instance_Iface()
        {
            var instance = new SomeClass();
            container.RegisterInstance<ISomeClass>(instance);
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(instance, map.Factory());
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);

            Assert.Throws<TypeAccessException>(() => container.GetInstance<SomeClass>());
            Assert.Equal(container.GetInstance<ISomeClass>(), map.Factory.Invoke());
            Assert.Equal(container.GetInstance(typeof(ISomeClass)), map.Factory.Invoke());
            Assert.Single(container.Maps());
        }

        [Fact]
        public void Test_Register_Factory()
        {
            container.RegisterFactory(() => new SomeClass());
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(null, map.ConcreteType);
            Assert.NotEqual(null, map.Factory);
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);
        }

        [Fact]
        public void Test_Register_Factory_Iface()
        {
            container.RegisterFactory<ISomeClass>(() => new SomeClass());
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(null, map.ConcreteType);
            Assert.NotEqual(null, map.Factory);
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);
        }

    }
}
