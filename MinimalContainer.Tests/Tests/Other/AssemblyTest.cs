using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Other
{
    public class AssemblyTest
    {
        public interface IFoo { }
        public class Bar { }

        private readonly Action<string> Write;
        public AssemblyTest(ITestOutputHelper output) => Write = output.WriteLine;

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
