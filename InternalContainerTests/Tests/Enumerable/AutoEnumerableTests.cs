using System;
using System.Collections.Generic;
using System.Reflection;
using InternalContainer;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainerTests.Tests.Enumerable
{
    public class EnumerableTest
    {
        internal interface INotUsed { }
        internal interface IMarker { }
        internal class ClassA : IMarker { }
        internal class ClassB : IMarker { }
        private readonly Container container;
        private readonly Action<string> write;

        public EnumerableTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(Lifestyle.Singleton, log:output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Get_Enumerable_Concrete()
        {
            var list = container.GetInstance<IList<ClassA>>();
            Assert.Equal(1, list.Count);
            Assert.Equal(3, container.GetRegistrations().Count);
            write(Environment.NewLine + container);
        }

        [Fact]
        public void Test_Get_Enumerable_Interface()
        {
            var list = container.GetInstance<IList<IMarker>>();
            Assert.Equal(2, list.Count);
            write(Environment.NewLine + container);
        }

        [Fact]
        public void Test_List_Types()
        {
            container.GetInstance<IEnumerable<IMarker>>();
            container.GetInstance<IList<IMarker>>();
        }

        [Fact]
        public void Test_RegisterAll_Enumerable()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<IEnumerable<INotUsed>>());
        }

    }
}
