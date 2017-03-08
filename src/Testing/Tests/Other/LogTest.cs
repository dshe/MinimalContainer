using System.Reflection;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Other
{
    public class LogTest : TestBase
    {
        public LogTest(ITestOutputHelper output) : base(output) {}

        public interface IFoo {}
        public class Foo : IFoo {}

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
