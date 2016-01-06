using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class AutoSingletonTests
    {
        private readonly Container container;

        public AutoSingletonTests(ITestOutputHelper output)
        {
            container = new Container(Lifestyle.Singleton, log:output.WriteLine, assembly:Assembly.GetExecutingAssembly());
        }

        public interface ISomeClass {}
        public class SomeClass : ISomeClass {}

        [Fact]
        public void Test1()
        {
            container.GetInstance<SomeClass>();

        }
        [Fact]
        public void Test2()
        {
            container.GetInstance<ISomeClass>();
        }

    }
}
