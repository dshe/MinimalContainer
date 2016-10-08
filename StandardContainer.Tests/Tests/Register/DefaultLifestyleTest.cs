using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StandardContainer.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Register
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
            var c = new Container(log: write);
            Assert.Throws<TypeAccessException>(() => c.GetInstance<SomeClassA>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<SomeClass>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<IEnumerable<ISomeClass>>()).Output(write);
        }

        [Fact]
        public void T02_Singleton()
        {
            var c = new Container(DefaultLifestyle.Singleton, log:write, assemblies:Assembly.GetExecutingAssembly());
            var instance = c.GetInstance<SomeClass>();
            var reg = c.GetRegistrations().Last();
            Assert.Equal(Style.Singleton, reg.Style);
            Assert.Equal(instance, c.GetInstance<SomeClass>());
        }

        [Fact]
        public void T03_Transient()
        {
            var c = new Container(DefaultLifestyle.Transient, log:write, assemblies: Assembly.GetExecutingAssembly());
            var instance = c.GetInstance<SomeClass>();
            var reg = c.GetRegistrations().Last();
            Assert.Equal(Style.Transient, reg.Style);
            Assert.NotEqual(instance, c.GetInstance<SomeClass>());
        }
    }
}
