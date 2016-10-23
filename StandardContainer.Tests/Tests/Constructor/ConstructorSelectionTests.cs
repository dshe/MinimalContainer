using System;
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
            public bool Ok;
            public ClassA(int i) { }
            public ClassA() { Ok = true; }
        }
        [Fact]
        public void T01_Class_With_Multiple_Constructors()
        {
            var instance = container.GetInstance<ClassA>();
            Assert.True(instance.Ok);
        }

        public class ClassB
        {
            public bool Ok;
            public ClassB() {}
            [ContainerConstructor]
            public ClassB(ClassA a) { Ok = true; }
        }
        [Fact]
        public void T02_Class_With_Attribute_Constructor()
        {
            var instance = container.GetInstance<ClassB>();
            Assert.True(instance.Ok);
        }

        public class ClassC
        {
            [ContainerConstructor]
            public ClassC(int i) { }
            [ContainerConstructor]
            public ClassC() { }
        }
        [Fact]
        public void T03_Class_With_Multiple_Attributes()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassC>()).Output(write);
        }

    }

}
