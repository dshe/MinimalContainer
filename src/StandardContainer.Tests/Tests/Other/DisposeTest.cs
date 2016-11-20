using System;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
{
    public class DisposeTest : TestBase
    {
        public class ClassA : IDisposable
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
            var container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: Write);
            container.RegisterSingleton<ClassA>();
            var instance = container.Resolve<ClassA>();
            container.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public void T02_Dispose_Instance()
        {
            var container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: Write);
            var instance = new ClassA();
            container.RegisterInstance(instance);
            container.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public void T03_Dispose_Other()
        {
            var container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: Write);
            container.RegisterTransient<ClassA>();
            var instance = container.Resolve<ClassA>();
            container.Dispose();
            Assert.False(instance.IsDisposed);

            container.RegisterFactory(() => instance);
            container.Dispose();
            Assert.False(instance.IsDisposed);
        }

    }
}
