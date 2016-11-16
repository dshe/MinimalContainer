using System;
using System.Collections.Generic;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
{
    public class AssemblyTest
    {
        private readonly Container container;
        private readonly Action<string> write;

        public AssemblyTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: write, assemblies: typeof(string).Assembly);
        }

        public interface IClassA { }
        public class ClassA { }

        [Fact]
        public void T01_No_Assembly_Register()
        {
            container.RegisterSingleton<ClassA>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IClassA>()).Output(write);
        }

        [Fact]
        public void T02_No_Assembly_GetInstance()
        {
            container.Resolve<ClassA>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<IClassA>()).Output(write);
        }

        [Fact]
        public void T03_No_Assembly_GetInstance_List()
        {
            Assert.Throws<TypeAccessException>(() => container.Resolve<IList<ClassA>>()).Output(write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IList<IClassA>>()).Output(write);
        }

    }
}
