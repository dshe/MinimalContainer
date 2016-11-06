using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public class CaptiveDependencyTests
    {
        private readonly Container container;
        private readonly Action<string> write;

        public CaptiveDependencyTests(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(log: output.WriteLine);
        }


        public class ClassA
        {
            public ClassA(ClassB b) {}
        }

        public class ClassB {}


        [Fact]
        public void Test_CaptiveDependency_Singleton_Transient()
        {
            container.RegisterSingleton<ClassA>();
            container.RegisterTransient<ClassB>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassA>()).Output(write);
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Singleton()
        {
            container.RegisterTransient<ClassA>();
            container.RegisterSingleton<ClassB>();
            container.Resolve<ClassA>();
        }

        [Fact]
        public void Test_CaptiveDependency_Singleton_Singleton()
        {
            container.RegisterSingleton<ClassA>();
            container.RegisterSingleton<ClassB>();
            container.Resolve<ClassA>();
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Transient()
        {
            container.RegisterTransient<ClassA>();
            container.RegisterTransient<ClassB>();
            container.Resolve<ClassA>();
        }
    }
}
