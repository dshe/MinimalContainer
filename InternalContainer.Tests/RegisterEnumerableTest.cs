using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class RegisterAllTest
    {
        public interface ISomeClass { }
        public class SomeClass1 : ISomeClass { }
        public class SomeClass2 : ISomeClass { }
        private readonly Container container;

        public RegisterAllTest(ITestOutputHelper output)
        {
            container = new Container(log: output.WriteLine, assembly:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Register_Enumerable_Singleton()
        {
            container.RegisterSingleton<IEnumerable<ISomeClass>>();
            var map = container.Maps().Single();
            Assert.Equal(typeof(IEnumerable<ISomeClass>).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof(IEnumerable<ISomeClass>).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);
            Assert.Equal(false, map.AutoRegistered);
            Assert.Equal(0, map.InstancesCreated);

            var instance = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.IsAssignableFrom<IEnumerable<ISomeClass>>(instance);
            Assert.Equal(instance, container.GetInstance(typeof(IEnumerable<ISomeClass>)));
            Assert.Equal(3 ,container.Maps().Count);
            Assert.Equal(1, map.InstancesCreated);
        }

        [Fact]
        public void Test_Register_Enumerable_Transient()
        {
            container.RegisterTransient<IEnumerable<ISomeClass>>();
            var map = container.Maps().Single();

            var instance = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.NotEqual(instance, container.GetInstance(typeof(IEnumerable<ISomeClass>)));
            Assert.Equal(3, container.Maps().Count);
            Assert.Equal(2, map.InstancesCreated);
        }

        [Fact]
        public void Test_Register_List_Types()
        {
            container.RegisterSingleton<IEnumerable<ISomeClass>>();
            container.GetInstance<IEnumerable<ISomeClass>>();
            container.Dispose();

            container.RegisterSingleton<IList<ISomeClass>>();
            container.GetInstance<IList<ISomeClass>>();
            container.Dispose();

            container.RegisterSingleton<List<ISomeClass>>();
            container.GetInstance<List<ISomeClass>>();
            container.Dispose();
        }

    }
}
