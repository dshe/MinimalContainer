using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class AssemblyTest
    {
        public interface IClassA {}
        public class ClassA {}

        private readonly Container container;

        public AssemblyTest(ITestOutputHelper output)
        {
            container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: output.WriteLine, assemblies: typeof(string).Assembly);
        }

        [Fact]
        public void No_Assembly_Register()
        {
            container.RegisterSingleton<ClassA>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IClassA>());
        }

        [Fact]
        public void No_Assembly_GetInstance()
        {
            container.GetInstance<ClassA>();
            Assert.Throws<TypeAccessException>(() => container.GetInstance<IClassA>());
        }

        [Fact]
        public void No_Assembly_GetInstance_List()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<IList<ClassA>>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<IList<IClassA>>());
        }

    }
}
