using System;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Lifestyle
{
    public class SingletonTest
    {
        private readonly Action<string> write;
        public SingletonTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        [Fact]
        public void T01_Concrete()
        {
            var container = new Container(log: write);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).Output(write);
            Assert.Equal(container.GetInstance<SomeClass>(), container.GetInstance<SomeClass>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ISomeClass>()).Output(write);
        }

        [Fact]
        public void T02_Interface()
        {
            var container = new Container(log: write);
            container.RegisterSingleton<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ISomeClass>()).Output(write); ;
            Assert.Equal(container.GetInstance<ISomeClass>(), container.GetInstance<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<SomeClass>()).Output(write);
        }

        [Fact]
        public void T03_Concrete_Interface()
        {
            var container = new Container(log: write);
            container.RegisterSingleton<ISomeClass>();
            container.RegisterSingleton<SomeClass>();
            Assert.Equal(container.GetInstance<SomeClass>(), container.GetInstance<ISomeClass>());
        }

        [Fact]
        public void T04_Register_Singleton()
        {
            var container = new Container(log: write);
            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.GetInstance<SomeClass>()).Output(write);
            container.RegisterSingleton<SomeClass>();
            Assert.Equal(container.GetInstance<ISomeClass>(), container.GetInstance<SomeClass>());
        }

        [Fact]
        public void T05_Register_Singleton_Auto()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: write);
            Assert.Equal(container.GetInstance<ISomeClass>(), container.GetInstance<SomeClass>());
        }

    }
}
