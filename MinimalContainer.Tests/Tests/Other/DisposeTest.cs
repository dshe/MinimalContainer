using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;
using MinimalContainer.Tests.Utility;
using System;
using Xunit;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Other
{
    public class DisposeTest : TestBase
    {
        public class Foo : IDisposable
        {
            public bool IsDisposed;
            public void Dispose() => IsDisposed = true;
        }

        public DisposeTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_Dispose_Singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, loggerFactory: LoggerFactory);
            container.RegisterSingleton<Foo>();
            var instance = container.Resolve<Foo>();
            container.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public void T02_Dispose_Instance()
        {
            var container = new Container(DefaultLifestyle.Singleton, LoggerFactory);
            var instance = new Foo();
            container.RegisterInstance(instance);
            container.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public void T03_Dispose_Other()
        {
            var container = new Container(DefaultLifestyle.Singleton, LoggerFactory);
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
