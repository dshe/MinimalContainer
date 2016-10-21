using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Examples
{
    public class Examples
    {
        public interface IFoo { }
        public class Foo : IFoo { }
        private readonly Action<string> write;
        public Examples(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void Test_Usage()
        {
            var container = new Container(DefaultLifestyle.Singleton, log:write);

            container.RegisterSingleton<IFoo, Foo>();

            var instance = container.GetInstance<IFoo>();
            Assert.IsType<Foo>(instance);
            Assert.Equal(instance, container.GetInstance<IFoo>());

            write(container.ToString());
        }
    }
}
