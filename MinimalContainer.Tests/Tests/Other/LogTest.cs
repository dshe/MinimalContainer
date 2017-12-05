using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Other
{
    public class LogTest
    {
        public interface IFoo { }
        public class Foo : IFoo { }

        private readonly Action<string> Write;
        public LogTest(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void T01()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write, typeof(string).GetTypeInfo().Assembly);
            container.RegisterSingleton<IFoo, Foo>();
            Write("");
            Write(container.ToString());
        }

        [Fact]
        public void T02()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write, typeof(string).GetTypeInfo().Assembly);
            container.RegisterSingleton<IFoo, Foo>();
            Write("");
            container.Log();
        }

    }
}
