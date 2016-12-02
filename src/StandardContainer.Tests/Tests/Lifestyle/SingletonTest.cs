using System;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Lifestyle
{
    public class SingletonTest : TestBase
    {
        public SingletonTest(ITestOutputHelper output) : base(output) {}

        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        [Fact]
        public void T01_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).WriteMessageTo(Write);
            Assert.Equal(container.Resolve<SomeClass>(), container.Resolve<SomeClass>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ISomeClass>()).WriteMessageTo(Write);
            Assert.Equal(container.Resolve<ISomeClass>(), container.Resolve<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T03_Concrete_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ISomeClass>();
            container.RegisterSingleton<SomeClass>();
            Assert.Equal(container.Resolve<SomeClass>(), container.Resolve<ISomeClass>());
        }

        [Fact]
        public void T04_Register_Singleton()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>()).WriteMessageTo(Write);
            container.RegisterSingleton<SomeClass>();
            Assert.Equal(container.Resolve<ISomeClass>(), container.Resolve<SomeClass>());
        }

        [Fact]
        public void T05_Register_Singleton_Auto()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            Assert.Equal(container.Resolve<ISomeClass>(), container.Resolve<SomeClass>());
        }

    }
}
