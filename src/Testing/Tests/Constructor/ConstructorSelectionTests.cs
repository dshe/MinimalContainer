using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Constructor
{
    public class ConstructorSelectionTests : TestBase
    {
        public ConstructorSelectionTests(ITestOutputHelper output) : base(output) {}

        public class ClassA
        {
            public bool Ok;
            public ClassA(int i) { }
            public ClassA() { Ok = true; }
        }
        [Fact]
        public void T01_Class_With_Multiple_Constructors()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            var instance = container.Resolve<ClassA>();
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
            var container = new Container(DefaultLifestyle.Singleton, Write);
            var instance = container.Resolve<ClassB>();
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
            var container = new Container(DefaultLifestyle.Singleton, Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassC>()).WriteMessageTo(Write);
        }

    }

}
