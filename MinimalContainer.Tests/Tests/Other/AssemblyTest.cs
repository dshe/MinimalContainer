using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;
using Microsoft.Extensions.Logging;
using Divergic.Logging.Xunit;

namespace MinimalContainer.Tests.Other
{
    public class AssemblyTest : TestBase
    {
        public interface IFoo { }
        public class Bar { }

        public AssemblyTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_No_Assembly_Register()
        {
            var container = new Container(DefaultLifestyle.Singleton, LoggerFactory, typeof(string).GetTypeInfo().Assembly);
            container.Resolve<Bar>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<IFoo>()).WriteMessageTo(Logger);
        }

        [Fact]
        public void T03_No_Assembly_GetInstance_List()
        {
            var container = new Container(DefaultLifestyle.Singleton, LoggerFactory, typeof(string).GetTypeInfo().Assembly);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IList<Bar>>()).WriteMessageTo(Logger);
        }

    }
}
