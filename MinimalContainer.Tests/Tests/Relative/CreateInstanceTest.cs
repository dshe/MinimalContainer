using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Relative
{
    public class CreateInstanceTest
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

        private readonly Action<string> Write;
        public CreateInstanceTest(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void Test_Cannot_Create_Dependency()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassZ>()).WriteMessageTo(Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassY>()).WriteMessageTo(Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassX>()).WriteMessageTo(Write);
        }
    }
}
