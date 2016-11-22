using System;
using System.Collections.Generic;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
{
    public class AssemblyTest : TestBase
    {
        public AssemblyTest(ITestOutputHelper output) : base(output) {}

        public interface IClassA { }
        public class ClassA { }

        [Fact]
        public void T01_No_Assembly_Register()
        {
            var container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: Write, assemblies: typeof(string).Assembly);
            container.Resolve<ClassA>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<IClassA>()).Output(Write);
        }

        [Fact]
        public void T03_No_Assembly_GetInstance_List()
        {
            var container = new Container(defaultLifestyle: DefaultLifestyle.Singleton, log: Write, assemblies: typeof(string).Assembly);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IList<ClassA>>()).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IList<IClassA>>()).Output(Write);
        }

    }
}
