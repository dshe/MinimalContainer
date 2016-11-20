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
        public void T01_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<SomeClass>()).Output(Write);
            var instance1 = container.Resolve<SomeClass>();
            var instance2 = container.Resolve<SomeClass>();
            Assert.NotEqual(instance1, instance2);
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>()).Output(Write);
        }

        [Fact]
        public void T02_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterTransient<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<ISomeClass>()).Output(Write);
            var instance3 = container.Resolve<ISomeClass>();
            var instance4 = container.Resolve<ISomeClass>();
            Assert.NotEqual(instance3, instance4);
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>()).Output(Write);
        }

        [Fact]
        public void T03_Concrete_Interface()
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
