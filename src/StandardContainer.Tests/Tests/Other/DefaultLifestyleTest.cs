using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
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
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClassA>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<ISomeClass>>()).Output(write);
        }

        [Fact]
        public void T02_Singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, log:write);
            Assert.Equal(container.Resolve<SomeClass>(), container.Resolve<SomeClass>());
        }

        [Fact]
        public void T03_Transient()
        {
            var container = new Container(DefaultLifestyle.Transient, log:write);
            Assert.NotEqual(container.Resolve<SomeClass>(), container.Resolve<SomeClass>());
        }
    }
}
