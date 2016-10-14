using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using StandardContainer;

namespace StandardContainer.Tests.Tests.Extensions
{
    public class ExtensionsTest
    {
        public ExtensionsTest(ITestOutputHelper output)
        {
        }

        [Fact]
        public void T01_No_Constructor()
        {
            var type = typeof(int);
            Assert.Throws<TypeAccessException>(() => type.GetTypeInfo().GetConstructor());
        }

        [Fact]
        public void T01_GetInstance_Null()
        {
            var container = new Container();
            Assert.Throws<ArgumentNullException>(() => container.GetInstance(null));
        }

        [Fact]
        public void T01_Register_Factory_Null()
        {
            var container = new Container();
            Assert.Throws<ArgumentNullException>(() => container.RegisterFactory(typeof(object), null));
        }

        [Fact]
        public void T01_Register_Instance_Null()
        {
            var container = new Container();
            Assert.Throws<ArgumentNullException>(() => container.RegisterInstance(typeof(object), null));
        }


    }
}
