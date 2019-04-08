using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Relative
{
    public class CaptiveDependencyTests : BaseUnitTest
    {
        public class Foo
        {
            public Foo(Bar b) { }
        }

        public class Bar { }

        public CaptiveDependencyTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test_CaptiveDependency_Singleton_Transient()
        {
            var container = new Container(logger: Logger);
            container.RegisterSingleton<Foo>();
            container.RegisterTransient<Bar>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>()).WriteMessageTo(Logger);
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Singleton()
        {
            var container = new Container(logger: Logger);
            container.RegisterTransient<Foo>();
            container.RegisterSingleton<Bar>();
            container.Resolve<Foo>();
        }

        [Fact]
        public void Test_CaptiveDependency_Singleton_Singleton()
        {
            var container = new Container(logger: Logger);
            container.RegisterSingleton<Foo>();
            container.RegisterSingleton<Bar>();
            container.Resolve<Foo>();
        }

        [Fact]
        public void Test_CaptiveDependency_Transient_Transient()
        {
            var container = new Container(logger: Logger);
            container.RegisterTransient<Foo>();
            container.RegisterTransient<Bar>();
            container.Resolve<Foo>();
        }
    }
}
