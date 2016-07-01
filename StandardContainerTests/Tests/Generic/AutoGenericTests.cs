using System;
using System.Reflection;
using StandardContainer;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainerTests.Tests.Generic
{
    public class AutoGenericTests
    {
        internal interface IClassA {}
        internal class ClassA : IClassA {}
        internal class ClassB<T>
        {
            public ClassB(T t) {}
        }

        internal class ClassC
        {
            public ClassC(ClassB<ClassA> ba)
            { }
        }
        internal class ClassD
        {
            public ClassD(ClassB<IClassA> ba)
            { }
        }

        private readonly Container container;
        private readonly Action<string> write;

        public AutoGenericTests(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(Container.Lifestyle.Singleton, log:output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_01()
        {
            container.GetInstance<ClassC>();
            Assert.Equal(4, container.GetRegistrations().Count);
            write(Environment.NewLine + container);
        }

        [Fact]
        public void Test_02()
        {
            container.GetInstance<ClassD>();
            Assert.Equal(4, container.GetRegistrations().Count);
            write(Environment.NewLine + container);
        }



    }
}
