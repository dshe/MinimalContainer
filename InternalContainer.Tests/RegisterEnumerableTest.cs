using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class RegisterEnumerableTest
    {
        public interface ISomeClass {}
        public class SomeClass1 : ISomeClass {}
        public class SomeClass2 : ISomeClass {}

        private readonly Container container;

        public RegisterEnumerableTest(ITestOutputHelper output)
        {
            container = new Container(log: output.WriteLine, assembly: Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Register_Enumerable_Singleton()
        {
            container.RegisterSingleton<IEnumerable<ISomeClass>>();
            var map = container.Maps().Single();
            Assert.Equal(typeof (IEnumerable<ISomeClass>).GetTypeInfo(), map.SuperType);
            Assert.Equal(typeof (IEnumerable<ISomeClass>).GetTypeInfo(), map.ConcreteType);
            Assert.Equal(null, map.Factory);
            Assert.Equal(Lifestyle.Singleton, map.Lifestyle);
            Assert.Equal(false, map.AutoRegistered);
            Assert.Equal(0, map.Instances);

            var instance = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.IsAssignableFrom<IEnumerable<ISomeClass>>(instance);
            Assert.Equal(instance, container.GetInstance(typeof (IEnumerable<ISomeClass>)));
            Assert.Equal(3, container.Maps().Count);
            foreach (var m in container.Maps())
            {
                Assert.Equal(Lifestyle.Singleton, m.Lifestyle);
                Assert.Equal(1, m.Instances);
            }
        }

        [Fact]
        public void Test_Register_Enumerable_Transient()
        {
            container.RegisterTransient<IEnumerable<ISomeClass>>();

            var instance = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.NotEqual(instance, container.GetInstance(typeof (IEnumerable<ISomeClass>)));
            Assert.Equal(3, container.Maps().Count);
            foreach (var m in container.Maps())
            {
                Assert.Equal(Lifestyle.Transient, m.Lifestyle);
                Assert.Equal(2, m.Instances);
            }
        }

        [Fact]
        public void Test_Register_List_Types()
        {
            container.RegisterSingleton<IEnumerable<ISomeClass>>();
            var list = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.Equal(2, list.Count());
            container.Dispose();

            container.RegisterSingleton<IList<ISomeClass>>();
            list = container.GetInstance<IList<ISomeClass>>();
            Assert.Equal(2, list.Count());
            container.Dispose();

            container.RegisterSingleton<IList<ISomeClass>>();
            list = container.GetInstance<IList<ISomeClass>>();
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public void Test_Register_List_Concrete()
        {
            container.RegisterSingleton<IEnumerable<SomeClass1>>();
            var list = container.GetInstance<IEnumerable<SomeClass1>>();
            Assert.Equal(1, list.Count());
        }
    }
}
