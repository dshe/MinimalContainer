using System.Linq;
using System.Reflection;
using Xunit;

namespace InternalContainer.Tests
{
    public class RegisterTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }
        private readonly Container container = new Container();

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
            Assert.Equal(instance, container.GetInstance(typeof(SomeClass)));
            Assert.Single(container.Maps());
            Assert.Equal(1, map.InstancesCreated);
        }

        [Fact]
        public void Test_Register_Singleton_Interface()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);

            var instance = container.GetInstance<ISomeClass>();
            Assert.IsType<SomeClass>(instance);
            Assert.Equal(instance, container.GetInstance(typeof(ISomeClass)));
            Assert.Single(container.Maps());
            Assert.Equal(1, map.InstancesCreated);
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

            var instance = container.GetInstance<SomeClass>();
            Assert.IsType<SomeClass>(instance);
            Assert.NotEqual(instance, container.GetInstance(typeof(SomeClass)));
            Assert.Single(container.Maps());
            Assert.Equal(2, map.InstancesCreated);
        }

        [Fact]
        public void Test_Register_Transient_Interface()
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
            Assert.Equal(2, map.InstancesCreated);
        }

        [Fact]
        public void Test_Register_Instance()
        {
            var instance = new SomeClass();
            container.RegisterInstance(instance);
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(instance, map.Factory());
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);

            Assert.Equal(instance, container.GetInstance<SomeClass>());
            Assert.Equal(instance, map.Factory());
            Assert.Single(container.Maps());
            Assert.Equal(0, map.InstancesCreated);
        }

        [Fact]
        public void Test_Register_Instance_Iface()
        {
            var instance = new SomeClass();
            container.RegisterInstance<ISomeClass>(instance);
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(instance, map.Factory());
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);

            Assert.Equal(instance, container.GetInstance<ISomeClass>());
            Assert.Equal(instance, map.Factory());
            Assert.Single(container.Maps());
            Assert.Equal(0, map.InstancesCreated);
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

            var instance = container.GetInstance<SomeClass>();
            Assert.NotEqual(instance, container.GetInstance<SomeClass>());
            Assert.Single(container.Maps());
            Assert.Equal(0, map.InstancesCreated);
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

            var instance = container.GetInstance<ISomeClass>();
            Assert.NotEqual(instance, container.GetInstance<ISomeClass>());
            Assert.Single(container.Maps());
            Assert.Equal(0, map.InstancesCreated);
        }
    }
}
