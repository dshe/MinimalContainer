using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Register
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
            container = new Container(log: output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Register_Singleton()
        {
            container.RegisterSingleton<SomeClass>();
            var reg = container.GetRegistrations().Last();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.SuperType);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(Lifestyle.Singleton, reg.Lifestyle);

            var instance = container.GetInstance<SomeClass>();
            Assert.IsType<SomeClass>(instance);
            Assert.Equal(instance, container.GetInstance(typeof(SomeClass)));
            output.WriteLine(container.ToString());
        }

        [Fact]
        public void Test_Register_Singleton_Interface()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            var reg = container.GetRegistrations().Last();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(Lifestyle.Singleton, reg.Lifestyle);

            var instance = container.GetInstance<ISomeClass>();
            Assert.IsType<SomeClass>(instance);
            Assert.Equal(instance, container.GetInstance(typeof(ISomeClass)));
            output.WriteLine(container.ToString());
        }

        [Fact]
        public void Test_Register_Transient()
        {
            container.RegisterTransient<SomeClass>();
            var reg = container.GetRegistrations().Last();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.SuperType);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(Lifestyle.Transient, reg.Lifestyle);

            var instance = container.GetInstance<SomeClass>();
            Assert.IsType<SomeClass>(instance);
            Assert.NotEqual(instance, container.GetInstance(typeof(SomeClass)));
            output.WriteLine(container.ToString());
        }

        [Fact]
        public void Test_Register_Transient_Interface()
        {
            container.RegisterTransient<ISomeClass, SomeClass>();
            var reg = container.GetRegistrations().Last();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(Lifestyle.Transient, reg.Lifestyle);

            var instance = container.GetInstance<ISomeClass>();
            Assert.IsType<SomeClass>(instance);
            Assert.NotEqual(instance, container.GetInstance<ISomeClass>());
            output.WriteLine(container.ToString());
        }

        [Fact]
        public void Test_Register_Instance()
        {
            var instance = new SomeClass();
            container.RegisterInstance(instance);
            var reg = container.GetRegistrations().Last();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(instance, reg.Instance);
            Assert.Equal(Lifestyle.Singleton, reg.Lifestyle);

            Assert.Equal(instance, container.GetInstance<SomeClass>());
            Assert.Equal(instance, reg.Instance);
            output.WriteLine(container.ToString());
        }

        [Fact]
        public void Test_Register_Instance_Iface()
        {
            var instance = new SomeClass();
            container.RegisterInstance<ISomeClass>(instance);
            var reg = container.GetRegistrations().Last();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.SuperType);
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(instance, reg.Instance);
            Assert.Equal(Lifestyle.Singleton, reg.Lifestyle);

            Assert.Equal(instance, container.GetInstance<ISomeClass>());
            Assert.Equal(instance, reg.Instance);
            output.WriteLine(container.ToString());
        }

        [Fact]
        public void Test_Register_Factory()
        {
            container.RegisterFactory(() => new SomeClass());
            var reg = container.GetRegistrations().Last();
            Assert.Equal(typeof(SomeClass).GetTypeInfo(), reg.SuperType);
            Assert.Equal(null, reg.ConcreteType);
            Assert.NotEqual(null, reg.Factory);
            Assert.Equal(Lifestyle.Transient, reg.Lifestyle);

            var instance = container.GetInstance<SomeClass>();
            Assert.NotEqual(instance, container.GetInstance<SomeClass>());
            output.WriteLine(container.ToString());
        }

        [Fact]
        public void Test_Register_Factory_Iface()
        {
            container.RegisterFactory<ISomeClass>(() => new SomeClass());
            var reg = container.GetRegistrations().Last();
            Assert.Equal(typeof(ISomeClass).GetTypeInfo(), reg.SuperType);
            Assert.Equal(null, reg.ConcreteType);
            Assert.NotEqual(null, reg.Factory);
            Assert.Equal(Lifestyle.Transient, reg.Lifestyle);

            var instance = container.GetInstance<ISomeClass>();
            Assert.NotEqual(instance, container.GetInstance<ISomeClass>());
            output.WriteLine(container.ToString());
        }
    }
}
