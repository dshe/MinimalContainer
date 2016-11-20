using System;
using System.Linq;
using System.Reflection;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public class CaptiveDependencyTests : TestBase
    {
        public CaptiveDependencyTests(ITestOutputHelper output) : base(output) {}

        public class ClassA
        {
            public ClassA(ClassB b) {}
        }

        public class ClassB {}


        [Fact]
        public void Test_CaptiveDependency_Singleton_Transient()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ClassA>();
            container.RegisterTransient<ClassB>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassA>()).Output(Write);
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Singleton()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<ClassA>();
            container.RegisterSingleton<ClassB>();
            container.Resolve<ClassA>();
        }

        [Fact]
        public void Test_CaptiveDependency_Singleton_Singleton()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ClassA>();
            container.RegisterSingleton<ClassB>();
            container.Resolve<ClassA>();
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Transient()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<ClassA>();
            container.RegisterTransient<ClassB>();
            container.Resolve<ClassA>();
        }
    }
}
