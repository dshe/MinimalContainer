using Microsoft.Extensions.Logging;
using MinimalContainer.Tests.Utility;
using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Other
{
    public class LogTest : TestBase
    {
        public interface IFoo { }
        public class Foo : IFoo { }

        public LogTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01()
        {
            var container = new Container(DefaultLifestyle.Singleton,LoggerFactory, typeof(string).GetTypeInfo().Assembly);
            container.RegisterSingleton<IFoo, Foo>();
            Logger.LogDebug("");
            Logger.LogDebug(container.ToString());
        }

        [Fact]
        public void T02()
        {
            var container = new Container(DefaultLifestyle.Singleton, LoggerFactory, typeof(string).GetTypeInfo().Assembly);
            container.RegisterSingleton<IFoo, Foo>();
            Logger.LogDebug("");
            container.Log();
        }

    }
}
