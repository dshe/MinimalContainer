using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class TransientTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Container container;

        public TransientTest(ITestOutputHelper output)
        {
            container = new Container(log: output.WriteLine);
        }

        [Fact]
        public void T01_Concrete()
        {
            container.RegisterTransient<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<SomeClass>());
            var instance1 = container.GetInstance<SomeClass>();
            var instance2 = container.GetInstance<SomeClass>();
            Assert.NotEqual(instance1, instance2);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ISomeClass>());
        }

        [Fact]
        public void T02_Interface()
        {
            container.RegisterTransient<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterTransient<ISomeClass>());
            var instance3 = container.GetInstance<ISomeClass>();
            var instance4 = container.GetInstance<ISomeClass>();
            Assert.NotEqual(instance3, instance4);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<SomeClass>());
        }

        [Fact]
        public void T03_Concrete_Interface()
        {
            container.RegisterTransient<ISomeClass>();
            container.RegisterTransient<SomeClass>();
            var instance5 = container.GetInstance<SomeClass>();
            var instance6 = container.GetInstance<ISomeClass>();
            Assert.NotEqual(instance6, instance5);
        }

    }
}
