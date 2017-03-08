using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Relative
{
    public class CaptiveDependencyTests : TestBase
    {
        public CaptiveDependencyTests(ITestOutputHelper output) : base(output) {}

        public class Foo
        {
            public Foo(Bar b) {}
        }

        public class Bar {}


        [Fact]
        public void Test_CaptiveDependency_Singleton_Transient()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<Foo>();
            container.RegisterTransient<Bar>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>()).WriteMessageTo(Write);
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Singleton()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<Foo>();
            container.RegisterSingleton<Bar>();
            container.Resolve<Foo>();
        }

        [Fact]
        public void Test_CaptiveDependency_Singleton_Singleton()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<Foo>();
            container.RegisterSingleton<Bar>();
            container.Resolve<Foo>();
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Transient()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<Foo>();
            container.RegisterTransient<Bar>();
            container.Resolve<Foo>();
        }
    }
}
