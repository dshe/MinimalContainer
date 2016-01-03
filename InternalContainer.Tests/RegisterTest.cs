using System;
using System.Linq;
using System.Reflection;
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
        public void Test01_Null_SuperType()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => container.Register(null, typeof(SomeClass), () => new SomeClass(), Lifestyle.Singleton));
            output.WriteLine(ex.Message);
        }
        [Fact]
        public void Test02_Register_Error_Null()
        {
            var ex = Assert.Throws<ArgumentException>(() => container.Register(typeof(ISomeClass), typeof(SomeClass), null, Lifestyle.AutoRegisterDisabled));
            output.WriteLine(ex.Message);
            Assert.Throws<ArgumentException>(() => container.RegisterInstance<SomeClass>(null));
        }

        [Fact]
        public void Test02_Register_Error_Abstract()
        {
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<ISomeClass>());
        }

        [Fact]
        public void Test03_Register_Error_Duplicate()
        {
            container.RegisterTransient<SomeClass>();
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<SomeClass>());
            container.Dispose();

            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<ISomeClass, SomeClass>());
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<SomeClass>());
        }



        [Fact]
        public void Test_Register_Singleton()
        {
            container.RegisterSingleton<SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperTypeInfo);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteTypeInfo);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);
        }

        [Fact]
        public void Test_Register_Transient()
        {
            container.RegisterTransient<SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperTypeInfo);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteTypeInfo);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);
        }

        [Fact]
        public void Test_Register_Singleton_Iface()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperTypeInfo);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteTypeInfo);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);
        }

        [Fact]
        public void Test_Register_Transient_Iface()
        {
            container.RegisterTransient<ISomeClass, SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperTypeInfo);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.ConcreteTypeInfo);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);
        }

        [Fact]
        public void Test_Register_Singleton_Instance()
        {
            var instance = new SomeClass();
            container.RegisterInstance(instance);
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperTypeInfo);
            Assert.Equal(instance, map.Factory());
            Assert.Equal(instance, map.Factory());
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);
        }

        [Fact]
        public void Test_Register_Singleton_Instance_Iface()
        {
            var instance = new SomeClass();
            container.RegisterInstance<ISomeClass>(instance);
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperTypeInfo);
            Assert.Equal(instance, map.Factory());
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);
        }

        [Fact]
        public void Test_Register_Transient_Factory()
        {
            container.RegisterFactory(() => new SomeClass());
            var map = container.Maps().Single();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), map.SuperTypeInfo);
            Assert.Equal(null, map.ConcreteTypeInfo);
            Assert.NotEqual(null, map.Factory);
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);
        }

        [Fact]
        public void Test_Register_Transient_Factory_Iface()
        {
            container.RegisterFactory<ISomeClass>(() => new SomeClass());
            var map = container.Maps().Single();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), map.SuperTypeInfo);
            Assert.Equal(null, map.ConcreteTypeInfo);
            Assert.NotEqual(null, map.Factory);
            Assert.Equal(Lifestyle.Transient, map.Lifestyle);
        }

    }
}
