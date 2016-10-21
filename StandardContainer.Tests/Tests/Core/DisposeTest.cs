using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class DisposeTest
    {
        public class ClassA : IDisposable
        {
            public bool IsDisposed = false;
            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private readonly Container container;

        public DisposeTest(ITestOutputHelper output)
        {
            container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: output.WriteLine);
        }

        [Fact]
        public void Dispose_Singleton()
        {
            container.RegisterSingleton<ClassA>();
            var instance = container.GetInstance<ClassA>();
            container.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public void Dispose_Instance()
        {
            var instance = new ClassA();
            container.RegisterInstance(instance);
            container.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public void Dispose_Other()
        {
            container.RegisterTransient<ClassA>();
            var instance = container.GetInstance<ClassA>();
            container.Dispose();
            Assert.False(instance.IsDisposed);

            container.RegisterFactory(() => instance);
            container.Dispose();
            Assert.False(instance.IsDisposed);
        }

    }
}
