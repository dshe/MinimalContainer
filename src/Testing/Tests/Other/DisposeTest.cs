using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Other
{
    public class DisposeTest : TestBase
    {
        public class Foo : IDisposable
        {
            public bool IsDisposed;
            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        public DisposeTest(ITestOutputHelper output) : base(output) {}

        [Fact]
        public void T01_Dispose_Singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            container.RegisterSingleton<Foo>();
            var instance = container.Resolve<Foo>();
            container.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public void T02_Dispose_Instance()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            var instance = new Foo();
            container.RegisterInstance(instance);
            container.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public void T03_Dispose_Other()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            container.RegisterTransient<Foo>();
            var instance = container.Resolve<Foo>();
            container.Dispose();
            Assert.False(instance.IsDisposed);

            container.RegisterFactory(() => instance);
            container.Dispose();
            Assert.False(instance.IsDisposed);
        }

    }
}
