using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using StandardContainer.Tests.Utility;

namespace StandardContainer.Tests.Other
{
    public class DefaultLifestyleTest
    {
        public class Foo {}
        public interface IBar { }
        public class Bar : IBar { }

        private readonly Action<string> Write;
        public DefaultLifestyleTest(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void T01_Unregistered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<Bar>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IBar>());
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<IBar>>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Singleton()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            Assert.Equal(container.Resolve<Bar>(), container.Resolve<Bar>());
        }

        [Fact]
        public void T03_Transient()
        {
            var container = new Container(DefaultLifestyle.Transient, Write);
            Assert.NotEqual(container.Resolve<Bar>(), container.Resolve<Bar>());
        }
    }
}
