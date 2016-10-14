using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
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

    public class RecursionTest
    {
        private readonly Container container;

        public RecursionTest(ITestOutputHelper output)
        {
            container = new Container(DefaultLifestyle.Singleton, log: output.WriteLine);
        }

        [Fact]
        public void Test_Recursive_Dependency()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<Class1>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<Class2>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<Class3>());
        }


    }
}
