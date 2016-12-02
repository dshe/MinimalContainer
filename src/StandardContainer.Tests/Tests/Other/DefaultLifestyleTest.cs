using System;
using System.Collections.Generic;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
{
    public class DefaultLifestyleTest : TestBase
    {
        public class SomeClassA {}
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        public DefaultLifestyleTest(ITestOutputHelper output) : base(output) {}

        [Fact]
        public void T01_Unregistered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClassA>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<ISomeClass>>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, log:Write);
            Assert.Equal(container.Resolve<SomeClass>(), container.Resolve<SomeClass>());
        }

        [Fact]
        public void T03_Transient()
        {
            var container = new Container(DefaultLifestyle.Transient, log:Write);
            Assert.NotEqual(container.Resolve<SomeClass>(), container.Resolve<SomeClass>());
        }
    }
}
