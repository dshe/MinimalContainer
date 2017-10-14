using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Lifestyle
{
    public class SingletonTest : TestBase
    {
        public SingletonTest(ITestOutputHelper output) : base(output) {}

        public interface IFoo { }
        public class Foo : IFoo { }

        [Fact]
        public void T01_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<Foo>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<Foo>()).WriteMessageTo(Write);
            Assert.Equal(container.Resolve<Foo>(), container.Resolve<Foo>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IFoo>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<IFoo>();
            //Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IFoo>()).WriteMessageTo(Write);

            //Assert.Equal(container.Resolve<IFoo>(), container.Resolve<IFoo>());
            
            
            //Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T03_Concrete_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<IFoo>();
            container.RegisterSingleton<Foo>();
            Assert.NotEqual(container.Resolve<Foo>(), container.Resolve<IFoo>());
        }

        [Fact]
        public void T04_Register_Singleton()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<IFoo, Foo>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>()).WriteMessageTo(Write);
            container.RegisterSingleton<Foo>();
            Assert.NotEqual(container.Resolve<IFoo>(), container.Resolve<Foo>());
        }

        [Fact]
        public void T05_Register_Singleton_Auto()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            Assert.NotEqual(container.Resolve<IFoo>(), container.Resolve<Foo>());
        }

    }
}
