using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class SingletonInterfaceTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Action<string> write;

        public SingletonInterfaceTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void T01_Register_Singleton()
        {
            var container = new Container(log: write);
            container.RegisterSingleton(typeof(ISomeClass), typeof(SomeClass));
            container.RegisterSingleton(typeof(SomeClass));
            Assert.Equal(container.GetInstance<ISomeClass>(), container.GetInstance<SomeClass>());
        }

        [Fact]
        public void T02_Register_Singleton_Auto()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: write);
            Assert.Equal(container.GetInstance<ISomeClass>(), container.GetInstance<SomeClass>());
        }

        [Fact]
        public void T03_Register_Singleton()
        {
            var container = new Container(log: write);
            container.RegisterSingleton(typeof(ISomeClass), typeof(SomeClass));
            container.RegisterInstance(new SomeClass());
            Assert.NotEqual(container.GetInstance<ISomeClass>(), container.GetInstance<SomeClass>());
        }



    }
}
