using System;
using System.Reactive.Subjects;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Relative
{
    public class ClassA
    {
        public ClassA(ClassB b) { }
    }

    public class ClassB {}

    public class CaptiveDependencyTests
    {
        private readonly Container container;

        public CaptiveDependencyTests(ITestOutputHelper output)
        {
            var subject = new Subject<string>();
            subject.Subscribe(output.WriteLine);
            container = new Container(observer: subject);
        }

        [Fact]
        public void Test_CaptiveDependency_Singleton_Transient()
        {
            container.RegisterSingleton<ClassA>();
            container.RegisterTransient<ClassB>();
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassA>());
            Assert.Equal(2, container.Dump().Count);
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Singleton()
        {
            container.RegisterTransient<ClassA>();
            container.RegisterSingleton<ClassB>();
            container.GetInstance<ClassA>();
            Assert.Equal(2, container.Dump().Count);
        }

        [Fact]
        public void Test_CaptiveDependency_Singleton_Singleton()
        {
            container.RegisterSingleton<ClassA>();
            container.RegisterSingleton<ClassB>();
            container.GetInstance<ClassA>();
            Assert.Equal(2, container.Dump().Count);
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Transient()
        {
            container.RegisterTransient<ClassA>();
            container.RegisterTransient<ClassB>();
            container.GetInstance<ClassA>();
            Assert.Equal(2, container.Dump().Count);
        }
    }

}
