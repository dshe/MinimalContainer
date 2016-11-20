using System;
using System.Collections.Generic;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
{
    public class RegisterErrorTest : TestBase
    {
        public RegisterErrorTest(ITestOutputHelper output) : base(output) {}

        public interface INoClass { }
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        [Fact]
        public void T00_Various_types()
        {
            var container = new Container(log: Write);
            Assert.Throws<ArgumentNullException>(() => container.RegisterSingleton(null)).Output(Write);
            Assert.Throws<ArgumentNullException>(() => container.RegisterFactory(typeof(object), null)).Output(Write);
            Assert.Throws<ArgumentNullException>(() => container.RegisterInstance(typeof(object), null)).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(int))).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(string))).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(42)).Output(Write);
            Assert.Throws<ArgumentNullException>(() => container.Resolve(null)).Output(Write);
            container.RegisterFactory(() => "string");
        }

        [Fact]
        public void T01_Abstract_No_Concrete()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<INoClass>()).Output(Write);
        }

        [Fact]
        public void T02_Not_Assignable()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(IDisposable), typeof(SomeClass))).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(typeof(int), 42)).Output(Write);
        }

        [Fact]
        public void T03_Duplicate_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).Output(Write);
        }

        [Fact]
        public void T04_Duplicate_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ISomeClass>()).Output(Write);
        }

        [Fact]
        public void T05_Duplicate_Concrete_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ISomeClass, SomeClass>();
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).Output(Write);
        }

        [Fact]
        public void T05_Duplicate_Type()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(new SomeClass())).Output(Write); ;
        }

        [Fact]
        public void T06_Unregistered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>()).Output(Write); ;
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>()).Output(Write); ;
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<ISomeClass>>()).Output(Write);
        }

    }
}
