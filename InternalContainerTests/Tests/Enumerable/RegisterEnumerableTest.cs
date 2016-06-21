using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InternalContainer;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainerTests.Tests.Enumerable
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
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterSingleton<IEnumerable<ISomeClass>>();

            var instance = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.Equal(instance, container.GetInstance(typeof (IEnumerable<ISomeClass>)));
            Assert.Equal(4, container.GetRegistrations().Count);
            foreach (var m in container.GetRegistrations())
            {
                Assert.Equal(Lifestyle.Singleton, m.Lifestyle);
            }
            container.Log();
        }

        [Fact]
        public void Test_Register_Enumerable_Transient()
        {
            container.RegisterTransient<IEnumerable<ISomeClass>>();
            container.RegisterTransient<SomeClass1>();
            container.RegisterTransient<SomeClass2>();

            var instance = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.NotEqual(instance, container.GetInstance(typeof (IEnumerable<ISomeClass>)));
            Assert.Equal(4, container.GetRegistrations().Count);
        }

        [Fact]
        public void Test_Register_List_Types()
        {
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterSingleton<IEnumerable<ISomeClass>>();
            var list = container.GetInstance<IEnumerable<ISomeClass>>();
            Assert.Equal(2, list.Count());
            container.Dispose();

            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterSingleton<IList<ISomeClass>>();
            list = container.GetInstance<IList<ISomeClass>>();
            Assert.Equal(2, list.Count());
            container.Dispose();

            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterSingleton<IList<ISomeClass>>();
            list = container.GetInstance<IList<ISomeClass>>();
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public void Test_Register_List_Concrete()
        {
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<IEnumerable<SomeClass1>>();
            var list = container.GetInstance<IEnumerable<SomeClass1>>();
            Assert.Equal(1, list.Count());
        }
    }
}
