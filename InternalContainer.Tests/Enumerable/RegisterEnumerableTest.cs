using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Enumerable
{
    public class RegisterEnumerableTest
    {
        public interface ISomeClass {}
        public class SomeClass1 : ISomeClass {}
        public class SomeClass2 : ISomeClass {}

        private readonly Container container;

        public RegisterEnumerableTest(ITestOutputHelper output)
        {
            container = new Container(log: output.WriteLine, assemblies: Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Register_Enumerable_Singleton()
        {
            container.RegisterSingleton<IEnumerable<ISomeClass>>();
            /*
            var reg = container.Registrations().Single();
            Assert.Equal(typeof (IEnumerable<ISomeClass>).GetTypeInfo(), reg.SuperType);
            Assert.Equal(typeof (IEnumerable<ISomeClass>).GetTypeInfo(), reg.ConcreteType);
            Assert.Equal(null, reg.Factory);
            Assert.Equal(Lifestyle.Singleton, reg.Lifestyle);
            Assert.Equal(false, reg.AutoRegistered);
            Assert.Equal(0, reg.Instances);
            */

            var instance = container.GetInstance<IEnumerable<ISomeClass>>();
            /*
            Assert.IsAssignableFrom<IEnumerable<ISomeClass>>(instance);
            Assert.Equal(instance, container.GetInstance(typeof (IEnumerable<ISomeClass>)));
            //Assert.Equal(3, container.Registrations().Count);
            foreach (var m in container.Registrations())
            {
                Assert.Equal(Lifestyle.Singleton, m.Lifestyle);
                Assert.Equal(1, m.Instances);
            }
            */
            container.Log();
        }

        [Fact]
        public void Test_Register_Enumerable_Transient()
        {
            container.RegisterTransient<IEnumerable<ISomeClass>>();

            var instance = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.NotEqual(instance, container.GetInstance(typeof (IEnumerable<ISomeClass>)));
            Assert.Equal(3, container.Registrations().Count);
            foreach (var m in container.Registrations())
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
