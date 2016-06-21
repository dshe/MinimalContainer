using System;
using System.Reflection;
using InternalContainer;
using InternalContainerTests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainerTests.Tests.Register
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
        public void Test04_Abstract_NoConcrete()
        {
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<INoClass>());
        }

        [Fact]
        public void Test05_Not_Assignable()
        {
            Assert.Throws<TypeAccessException>(() => 
                container.RegisterSingleton(typeof(IDisposable), typeof(SomeClass))).Output(write);
        }

        [Fact]
        public void Test06_Duplicate_Super()
        {
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).Output(write);
        }

        [Fact]
        public void Test07_Duplicate_Super_Interface()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ISomeClass, SomeClass>()).Output(write);

        }

        [Fact]
        public void Test08_Duplicate_Concrete()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).Output(write);
        }

        [Fact]
        public void Test09_Generic()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).Output(write);
        }
    }
}
