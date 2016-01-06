using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class EnumerableTest
    {
        internal interface INotUsed { }
        internal interface IMarker { }
        internal class ClassA : IMarker { }
        internal class ClassB : IMarker { }
        private readonly Container container;

        public EnumerableTest(ITestOutputHelper output)
        {
            container = new Container(Lifestyle.Singleton, log:output.WriteLine, assembly:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Get_Enumerable_Concrete()
        {
            var list = container.GetInstance<IList<ClassA>>();
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void Test_Get_Enumerable_Interface()
        {
            var list = container.GetInstance<IList<IMarker>>();
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void Test_List_Types()
        {
            container.GetInstance<IEnumerable<IMarker>>();
            container.GetInstance<IList<IMarker>>();
            container.GetInstance<List<IMarker>>();
        }

        [Fact]
        public void Test_RegisterAll_Enumerable()
        {
            container.GetInstance<IEnumerable<INotUsed>>();
        }

    }
}
