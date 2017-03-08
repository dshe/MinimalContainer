using System;
using System.Collections.Generic;
using System.Reflection;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Other
{
    public class AssemblyTest : TestBase
    {
        public AssemblyTest(ITestOutputHelper output) : base(output) {}

        public interface IFoo { }
        public class Bar { }

        [Fact]
        public void T01_No_Assembly_Register()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write, typeof(string).GetTypeInfo().Assembly);
            container.Resolve<Bar>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<IFoo>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T03_No_Assembly_GetInstance_List()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write, typeof(string).GetTypeInfo().Assembly);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IList<Bar>>()).WriteMessageTo(Write);
        }

    }
}
