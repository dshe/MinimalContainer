using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;
using Microsoft.Extensions.Logging;

namespace MinimalContainer.Tests.Relative
{
    public class RecursionTest : UnitTestBase
    {
        public class Class1
        {
            public Class1(Class2 c2) { }
        }

        public class Class2
        {
            public Class2(Class3 c3) { }
        }

        public class Class3
        {
            public Class3(Class1 c1) { }
        }

        public RecursionTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test_Recursive_Dependency()
        {
            var container = new Container(DefaultLifestyle.Singleton, Logger);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Class1>()).WriteMessageTo(Logger);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Class2>()).WriteMessageTo(Logger);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Class3>()).WriteMessageTo(Logger);
        }
    }
}
