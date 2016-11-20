using System;
using System.Collections.Generic;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
{
    public class LogTest : TestBase
    {
        public LogTest(ITestOutputHelper output) : base(output) {}

        public interface IClassA {}
        public class ClassA : IClassA {}

        [Fact]
        public void T01()
        {
            var container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: Write, assemblies: typeof(string).Assembly);
            container.RegisterSingleton<IClassA, ClassA>();
            Write("");
            Write(container.ToString());
        }

        [Fact]
        public void T02()
        {
            var container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: Write, assemblies: typeof(string).Assembly);
            container.RegisterSingleton<IClassA, ClassA>();
            Write("");
            container.Log();
        }

    }
}
