using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Enumerable
{
    public class EnumerableTest
    {
        internal interface INotUsed { }
        internal interface IMarker { }
        internal class ClassA : IMarker { }
        internal class ClassB : IMarker { }
        private readonly Container container;
        private readonly ITestOutputHelper output;

        public EnumerableTest(ITestOutputHelper output)
        {
            this.output = output;
            container = new Container(Lifestyle.Singleton, log:output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Get_Enumerable_Concrete()
        {
            var list = container.GetInstance<IList<ClassA>>();
            Assert.Equal(1, list.Count);
            Assert.Equal(2, container.Registrations().Count);
            output.WriteLine(Environment.NewLine + container);
        }

        [Fact]
        public void Test_Get_Enumerable_Interface()
        {
            var list = container.GetInstance<IList<IMarker>>();
            Assert.Equal(2, list.Count);
            output.WriteLine(Environment.NewLine + container);
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
