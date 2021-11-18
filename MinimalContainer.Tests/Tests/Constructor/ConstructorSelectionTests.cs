using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Constructor
{
    public class ConstructorSelectionTests : BaseUnitTest
    {
        public ConstructorSelectionTests(ITestOutputHelper output) : base(output) { }

        public class ClassA
        {
            public bool Ok;
            public ClassA(int i) { }
            public ClassA() { Ok = true; }
        }
        [Fact]
        public void T01_Class_With_Multiple_Constructors()
        {
            var container = new Container(DefaultLifestyle.Singleton, Log);
            var instance = container.Resolve<ClassA>();
            Assert.True(instance.Ok);
        }

        public class ClassB
        {
            public bool Ok;
            public ClassB() {}
            [ContainerConstructorAttribute]
            public ClassB(ClassA a) { Ok = true; }
        }
        [Fact]
        public void T02_Class_With_Attribute_Constructor()
        {
            var container = new Container(DefaultLifestyle.Singleton, Log);
            var instance = container.Resolve<ClassB>();
            Assert.True(instance.Ok);
        }

        public class ClassC
        {
            [ContainerConstructorAttribute]
            public ClassC(int i) { }
            [ContainerConstructorAttribute]
            public ClassC() { }
        }
        [Fact]
        public void T03_Class_With_Multiple_Attributes()
        {
            var container = new Container(DefaultLifestyle.Singleton, Log);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassC>()).WriteMessageTo(Log);
        }
    }
}
