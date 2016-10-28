using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
{
    public class LogTest
    {
        private readonly Container container;
        private readonly Action<string> write;

        public LogTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: write, assemblies: typeof(string).Assembly);
        }

        public interface IClassA {}
        public class ClassA : IClassA {}

        [Fact]
        public void T01()
        {
            container.RegisterSingleton<IClassA, ClassA>();
            write("");
            write(container.ToString());
        }

        [Fact]
        public void T02()
        {
            container.RegisterSingleton<IClassA, ClassA>();
            write("");
            container.Log();
        }

    }
}
