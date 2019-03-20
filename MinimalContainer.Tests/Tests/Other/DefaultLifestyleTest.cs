using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;
using Microsoft.Extensions.Logging;
using Divergic.Logging.Xunit;

namespace MinimalContainer.Tests.Other
{
    public class DefaultLifestyleTest : TestBase
    {
        public class Foo {}
        public interface IBar { }
        public class Bar : IBar { }

        public DefaultLifestyleTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_Unregistered()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<Bar>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IBar>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<IBar>>()).WriteMessageTo(Logger);
        }

        [Fact]
        public void T02_Singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, loggerFactory: LoggerFactory);
            Assert.Equal(container.Resolve<Bar>(), container.Resolve<Bar>());
        }

        [Fact]
        public void T03_Transient()
        {
            var container = new Container(DefaultLifestyle.Transient, loggerFactory: LoggerFactory);
            Assert.NotEqual(container.Resolve<Bar>(), container.Resolve<Bar>());
        }
    }
}
