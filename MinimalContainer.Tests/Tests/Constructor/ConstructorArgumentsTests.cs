using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Constructor
{
    public class ConstructorArgumentTests : BaseUnitTest
    {
        public class Class0 { }

        private readonly Container Container;

        public ConstructorArgumentTests(ITestOutputHelper output) : base(output)
             => Container = new Container(DefaultLifestyle.Singleton, Logger);

        public class Class1
        {
            public Class1(int i) { }
        }
        [Fact]
        public void T01_Value_Type()
        {
            Assert.Throws<TypeAccessException>(() => Container.Resolve<Class1>()).WriteMessageTo(Logger);
        }

        public class Class2
        {
            public Class2(int i = 42) { }
        }
        [Fact]
        public void T02_Value_Type_Default()
        {
            Container.Resolve<Class2>();
        }

        public class Class3
        {
            public Class0 class0;
            public Class3(Class0 c0) => class0 = c0;
        }
        [Fact]
        public void T03_Ref_Type()
        {
            var class3 = Container.Resolve<Class3>();
            Assert.NotNull(class3.class0);
        }

        public class Class4
        {
            public Class4(Class0 c0) { }
        }
        [Fact]
        public void T04_Ref_Type_Default()
        {
            Container.Resolve<Class4>();
        }

        public class Class5
        {
            public Class5(ref Class0 c0) { }
        }
        [Fact]
        public void T05_Ref_Type_Ref()
        {
            Assert.Throws<TypeAccessException>(() => Container.Resolve<Class5>()).WriteMessageTo(Logger);
        }

        public class Class6
        {
            public Class6(out Class0? c0) { c0 = null; }
        }
        [Fact]
        public void T06_Ref_Type_Out()
        {
            Assert.Throws<TypeAccessException>(() => Container.Resolve<Class6>()).WriteMessageTo(Logger);
        }
    }
}

