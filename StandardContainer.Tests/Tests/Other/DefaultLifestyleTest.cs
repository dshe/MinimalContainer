using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class DefaultLifestyleTest
    {
        public class SomeClassA {}
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Action<string> write;
        public DefaultLifestyleTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void T01_Unregistered()
        {
            var container = new Container(log: write);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<SomeClassA>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<SomeClass>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<IEnumerable<ISomeClass>>()).Output(write);
        }

        [Fact]
        public void T02_Singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, log:write);
            Assert.Equal(container.GetInstance<SomeClass>(), container.GetInstance<SomeClass>());
        }

        [Fact]
        public void T03_Transient()
        {
            var container = new Container(DefaultLifestyle.Transient, log:write);
            Assert.NotEqual(container.GetInstance<SomeClass>(), container.GetInstance<SomeClass>());
        }
    }
}
