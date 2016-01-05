using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class AutoTests
    {
        private readonly Container container;

        public AutoTests(ITestOutputHelper output)
        {
            container = new Container(Lifestyle.Singleton, log:output.WriteLine);
        }

        public interface ISomeClass {}
        public class SomeClass : ISomeClass {}

        [Fact]
        public void Test()
        {


        }
    }
}
