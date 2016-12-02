using System;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Lifestyle
{
    public class TransientTest : TestBase
    {
        public TransientTest(ITestOutputHelper output) : base(output) {}

        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        [Fact]
        public void T00_Not_Registered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T01_Already_Registered()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<SomeClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<SomeClass>();
            var instance1 = container.Resolve<SomeClass>();
            var instance2 = container.Resolve<SomeClass>();
            Assert.NotEqual(instance1, instance2);
        }

        [Fact]
        public void T03_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<ISomeClass>()).WriteMessageTo(Write);
            var instance3 = container.Resolve<ISomeClass>();
            var instance4 = container.Resolve<ISomeClass>();
            Assert.NotEqual(instance3, instance4);
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T04_Concrete_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<ISomeClass>();
            container.RegisterTransient<SomeClass>();
            var instance5 = container.Resolve<SomeClass>();
            var instance6 = container.Resolve<ISomeClass>();
            Assert.NotEqual(instance6, instance5);
        }

    }
}
