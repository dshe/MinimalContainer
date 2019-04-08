using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Relative
{
    public class CreateInstanceTest :BaseUnitTest
    {
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

        public CreateInstanceTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test_Cannot_Create_Dependency()
        {
            var container = new Container(DefaultLifestyle.Singleton, logger:Logger);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassZ>()).WriteMessageTo(Logger);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassY>()).WriteMessageTo(Logger);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassX>()).WriteMessageTo(Logger);
        }
    }
}
