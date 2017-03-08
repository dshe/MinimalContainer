using System;
using System.Collections.Generic;
using StandardContainer;
using Testing.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Testing.Tests.Other
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
            Assert.Throws<ArgumentNullException>(() => container.RegisterSingleton(null)).WriteMessageTo(Write);
            Assert.Throws<ArgumentNullException>(() => container.RegisterInstance(typeof(object), null)).WriteMessageTo(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(int))).WriteMessageTo(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(string))).WriteMessageTo(Write);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(42)).WriteMessageTo(Write);
            Assert.Throws<ArgumentNullException>(() => container.Resolve(null)).WriteMessageTo(Write);
        }

        [Fact]
        public void T01_Abstract_No_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<INoClass>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<INoClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Not_Assignable()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton(typeof(IDisposable), typeof(SomeClass));
            Assert.Throws<TypeAccessException>(() => container.Resolve<INoClass>()).WriteMessageTo(Write);
            //Assert.Throws<TypeAccessException>(() => container.RegisterInstance(typeof(int), 42)).WriteMessageTo(Write);
        }

        [Fact]
        public void T03_Duplicate_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T04_Duplicate_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ISomeClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T05_Duplicate_Concrete_Interface()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<ISomeClass, SomeClass>();
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T05_Duplicate_Type()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(new SomeClass())).WriteMessageTo(Write); ;
        }

        [Fact]
        public void T06_Unregistered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>()).WriteMessageTo(Write); ;
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>()).WriteMessageTo(Write); ;
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<ISomeClass>>()).WriteMessageTo(Write);
        }

    }
}
