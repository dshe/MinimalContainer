using MinimalContainer.Tests.Utility;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Other
{
    public class LogTest : BaseUnitTest
    {
        public interface IFoo { }
        public class Foo : IFoo { }

        public LogTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01()
        {
            var container = new Container(DefaultLifestyle.Singleton, Log, typeof(string).GetTypeInfo().Assembly);
            container.RegisterSingleton<IFoo, Foo>();
            Log("");
            Log(container.ToString());
        }

        [Fact]
        public void T02()
        {
            var container = new Container(DefaultLifestyle.Singleton, Log, typeof(string).GetTypeInfo().Assembly);
            container.RegisterSingleton<IFoo, Foo>();
            Log("");
            Log(container.ToString());
        }

    }
}
