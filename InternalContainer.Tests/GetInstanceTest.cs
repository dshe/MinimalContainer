using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class GetInstanceTest
    {
        private readonly Container container;

        public GetInstanceTest(ITestOutputHelper output)
        {
            container = new Container(Lifestyle.Singleton, log:output.WriteLine);
        }

        public interface ISomeClass {}
        public class SomeClass : ISomeClass {}

        [Fact]
        public void Test_GetInstance_Transient()
        {
            container.RegisterTransient<ISomeClass, SomeClass>();
            var instance = container.GetInstance<ISomeClass>();
            Assert.IsType<SomeClass>(instance);
            Assert.NotEqual(instance, container.GetInstance<ISomeClass>());
            Assert.Single(container.Maps());
        }




        [Fact]
        public void Test_GetInstance_Singleton()
        {
            // register concrete
            var instance = new SomeClass();
            container.RegisterInstance(instance);
            var map = container.Maps().Single();
            Assert.Equal(container.GetInstance<SomeClass>(), map.Factory.Invoke());
            Assert.Equal(container.GetInstance(typeof(SomeClass)), map.Factory.Invoke());
            Assert.Single(container.Maps());
            container.Dispose();

            // register abstract
            container.RegisterInstance<ISomeClass>(instance);
            map = container.Maps().Single();
            Assert.Equal(container.GetInstance<ISomeClass>(), map.Factory.Invoke());
            Assert.Equal(container.GetInstance(typeof(ISomeClass)), map.Factory.Invoke());
            Assert.Single(container.Maps());
        }

        [Fact]
        public void Test_GetInstance_With_Create()
        {
            // concrete
            container.RegisterSingleton<SomeClass>();
            var instance1 = container.GetInstance<SomeClass>();
            var map = container.Maps().Single();
            Assert.Equal(instance1, map.Factory.Invoke());
            Assert.Equal(container.GetInstance(typeof(SomeClass)), map.Factory.Invoke());
            Assert.Single(container.Maps());
            container.Dispose();

            //  with abstract
            container.RegisterSingleton<ISomeClass, SomeClass>();
            var instance2 = container.GetInstance<ISomeClass>();
            map = container.Maps().Single();
            Assert.Equal(instance2, map.Factory.Invoke());
            Assert.Equal(container.GetInstance(typeof(ISomeClass)), map.Factory.Invoke());
            Assert.Single(container.Maps());
        }

        [Fact]
        public void Test_Register_Concrete_Request_Abstract()
        {
            var instance = new SomeClass();
            container.RegisterInstance<SomeClass>(instance);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ISomeClass>());
        }

        [Fact]
        public void Test_Get_Concrete()
        {
            var instance = new SomeClass();
            container.RegisterInstance<ISomeClass>(instance);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<SomeClass>());
        }


    }
}
