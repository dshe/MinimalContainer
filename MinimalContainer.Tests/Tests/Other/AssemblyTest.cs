using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Other
{
    public class AssemblyTest : BaseUnitTest
    {
        public interface IFoo { }
        public class Bar { }

        public AssemblyTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_No_Assembly_Register()
        {
            var container = new Container(DefaultLifestyle.Singleton, Log, typeof(string).GetTypeInfo().Assembly);
            container.Resolve<Bar>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<IFoo>()).WriteMessageTo(Log);
        }

        [Fact]
        public void T03_No_Assembly_GetInstance_List()
        {
            var container = new Container(DefaultLifestyle.Singleton, Log, typeof(string).GetTypeInfo().Assembly);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IList<Bar>>()).WriteMessageTo(Log);
        }

    }
}
