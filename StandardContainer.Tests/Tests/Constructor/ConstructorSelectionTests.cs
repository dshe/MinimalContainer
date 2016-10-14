using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using StandardContainer.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Constructor
{
    public class ConstructorSelectionTests
    {
        private readonly Container container;
        private readonly Action<string> write;

        public ConstructorSelectionTests(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(DefaultLifestyle.Singleton, log: write);
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
            new ClassA(1);
        }

        public class ClassB
        {
            public ClassB(int i) {}
            [ContainerConstructor]
            public ClassB() {}
        }
        [Fact]
        public void Test_ClassWithAttributeConstructor()
        {
            container.GetInstance<ClassB>();
            new ClassB(1);
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
            new ClassC();
            new ClassC(1);
        }



    }

}
