using System;
using System.Collections.Generic;
using System.Reflection;
using StandardContainer.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class RegisterErrorTest
    {
        public interface INoClass { }
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }
        private readonly Container container;
        private readonly Action<string> write;

        public RegisterErrorTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(log: write,assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void T00_Various_types()
        {
            Assert.Throws<ArgumentNullException>(() => container.RegisterSingleton(null));
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(int)));
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(string)));
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(42));
            container.RegisterFactory(() => "string");
        }

        [Fact]
        public void T01_Abstract_No_Concrete()
        {
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<INoClass>()).Output(write);
        }

        [Fact]
        public void T02_Not_Assignable()
        {
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(IDisposable), typeof(SomeClass))).Output(write);
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(typeof(int), 42)).Output(write);
        }

        [Fact]
        public void T03_Duplicate_Concrete()
        {
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).Output(write);
        }

        [Fact]
        public void T04_Duplicate_Interface()
        {
            container.RegisterSingleton<ISomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ISomeClass>()).Output(write);
        }

        [Fact]
        public void T05_Duplicate_Concrete_Interface()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).Output(write);
        }

        [Fact]
        public void T05_Duplicate_Type()
        {
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterInstance(new SomeClass()));
        }

        [Fact]
        public void T06_Unregistered()
        {
            var c = new Container(log: write);
            Assert.Throws<TypeAccessException>(() => c.GetInstance<SomeClass>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<IEnumerable<ISomeClass>>()).Output(write);
        }

    }
}
