using System;
using System.Reflection;
using InternalContainer;
using InternalContainerTests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainerTests.Tests.Constructor
{
    public class ConstructorSelectionTests
    {
        private readonly Container container;
        private readonly Action<string> write;

        public ConstructorSelectionTests(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(autoLifestyle: Lifestyle.Singleton, log: write, assemblies: Assembly.GetExecutingAssembly());
        }

        public class ClassA
        {
            public ClassA(int i) { }
            public ClassA() { }
        }
        [Fact]
        public void Test_ClassWithMultipleConstructors()
        {
            container.GetInstance<ClassA>();
        }

        public class ClassB
        {
            public ClassB(int i) { }
            [ContainerConstructor]
            public ClassB() { }
        }
        [Fact]
        public void Test_ClassWithAttributeConstructor()
        {
            container.GetInstance<ClassB>();
        }

        public class ClassC
        {
            [ContainerConstructor]
            public ClassC(int i) { }
            [ContainerConstructor]
            public ClassC() { }
        }
        [Fact]
        public void Test_ClassWithMultipleAttributes()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassC>()).Output(write);
        }



    }

}
