using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Other
{
    public class DefaultLifestyleTest : BaseUnitTest
    {
        public class Foo {}
        public interface IBar { }
        public class Bar : IBar { }

        public DefaultLifestyleTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_Unregistered()
        {
            var container = new Container(log: Log);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<Bar>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IBar>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<IBar>>()).WriteMessageTo(Log);
        }

        [Fact]
        public void T02_Singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, Log);
            Assert.Equal(container.Resolve<Bar>(), container.Resolve<Bar>());
        }

        [Fact]
        public void T03_Transient()
        {
            var container = new Container(DefaultLifestyle.Transient, Log);
            Assert.NotEqual(container.Resolve<Bar>(), container.Resolve<Bar>());
        }
    }
}
