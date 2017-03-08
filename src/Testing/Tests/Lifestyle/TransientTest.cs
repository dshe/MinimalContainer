using System;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Lifestyle
{
    public class TransientTest : TestBase
    {
        public TransientTest(ITestOutputHelper output) : base(output) {}

        public interface IFoo { }
        public class Foo : IFoo { }

        [Fact]
        public void T00_Not_Registered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IFoo>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T01_Already_Registered()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<Foo>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<Foo>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<Foo>();
            var instance1 = container.Resolve<Foo>();
            var instance2 = container.Resolve<Foo>();
            Assert.NotEqual(instance1, instance2);
        }

        [Fact]
        public void T03_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<IFoo>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<IFoo>()).WriteMessageTo(Write);
            var instance3 = container.Resolve<IFoo>();
            var instance4 = container.Resolve<IFoo>();
            Assert.NotEqual(instance3, instance4);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T04_Concrete_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<IFoo>();
            container.RegisterTransient<Foo>();
            var instance5 = container.Resolve<Foo>();
            var instance6 = container.Resolve<IFoo>();
            Assert.NotEqual(instance6, instance5);
        }

    }
}
