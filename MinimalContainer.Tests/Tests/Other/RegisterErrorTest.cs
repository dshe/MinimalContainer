using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Other
{
    public class RegisterErrorTest : TestBase
    {
        public interface INoClass { }
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        public RegisterErrorTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T00_Various_types()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            Assert.Throws<ArgumentNullException>(() => container.RegisterInstance(typeof(object), null)).WriteMessageTo(Logger);
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(int))).WriteMessageTo(Logger);
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(string))).WriteMessageTo(Logger);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(42)).WriteMessageTo(Logger);
        }

        [Fact]
        public void T01_Abstract_No_Concrete()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            container.RegisterSingleton<INoClass>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<INoClass>()).WriteMessageTo(Logger);
        }

        [Fact]
        public void T02_Not_Assignable()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            container.RegisterSingleton(typeof(IDisposable), typeof(SomeClass));
            Assert.Throws<TypeAccessException>(() => container.Resolve<INoClass>()).WriteMessageTo(Logger);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(typeof(int), 42)).WriteMessageTo(Logger);
        }

        [Fact]
        public void T03_Duplicate_Concrete()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).WriteMessageTo(Logger);
        }

        [Fact]
        public void T04_Duplicate_Interface()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            container.RegisterSingleton<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ISomeClass>()).WriteMessageTo(Logger);
        }

        [Fact]
        public void T05_Duplicate_Concrete_Interface()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            container.RegisterSingleton<ISomeClass, SomeClass>();
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).WriteMessageTo(Logger);
        }

        [Fact]
        public void T05_Duplicate_Type()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(new SomeClass())).WriteMessageTo(Logger); ;
        }

        [Fact]
        public void T06_Unregistered()
        {
            var container = new Container(loggerFactory: LoggerFactory);
            Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>()).WriteMessageTo(Logger); ;
            Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>()).WriteMessageTo(Logger); ;
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<ISomeClass>>()).WriteMessageTo(Logger);
        }

    }
}
