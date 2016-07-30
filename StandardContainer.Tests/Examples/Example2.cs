using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Examples
{
    public class Examples
    {
        public interface IClassA { }
        public class ClassA : IClassA { }
        private readonly Action<string> write;
        public Examples(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void Test_Usage()
        {
            var container = new Container(Container.Lifestyle.Singleton, log:write);

            container.RegisterSingleton<IClassA, ClassA>();

            var instance = container.GetInstance<IClassA>();
            Assert.IsType<ClassA>(instance);
            Assert.Equal(instance, container.GetInstance<IClassA>());

            write("");
            container.GetRegistrations().Select(x => x.ToString()).ToList().ForEach(write);
            write("");

            container.Dispose();
        }
    }
}
