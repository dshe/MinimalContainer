using System;
using System.Reflection;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public class CreateInstanceTest : TestBase
    {
        public CreateInstanceTest(ITestOutputHelper output) : base(output) {}

        public class ClassX
        {
            public ClassX(ClassY y) { }
        }

        public class ClassY
        {
            public ClassY(ClassZ z) { }
        }

        public class ClassZ
        {
            public ClassZ(int x) { }
        }


        [Fact]
        public void Test_Cannot_Create_Dependency()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassZ>()).WriteMessageTo(Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassY>()).WriteMessageTo(Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassX>()).WriteMessageTo(Write);
        }
    }
}
