using System;
using System.Linq;
using System.Reflection;
using InternalContainer.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class RegisterErrorTest
    {
        public interface INoClass { }
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }
        private readonly Container container;
        private readonly ITestOutputHelper output;

        public RegisterErrorTest(ITestOutputHelper output)
        {
            this.output = output;
            container = new Container(log: output.WriteLine,assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test01_Null_SuperType()
        {
            //Assert.Throws<ArgumentNullException>(() => 
            //    container.Register(null, typeof(SomeClass), () => new SomeClass(), Lifestyle.Singleton)).Output(output);
        }
        [Fact]
        public void Test02_Null_ConcreteType()
        {
            //Assert.Throws<ArgumentNullException>(() =>
            //    container.Register(typeof(SomeClass), null, null, Lifestyle.Singleton)).Output(output);
        }
        [Fact]
        public void Test03_AutoLifestyleDisabled()
        {
            //Assert.Throws<ArgumentException>(() => 
            //    container.Register(typeof(ISomeClass), typeof(SomeClass), null, Lifestyle.AutoRegisterDisabled)).Output(output);
        }
        [Fact]
        public void Test04_Abstract_NoConcrete()
        {
             Assert.Throws<TypeAccessException>(() =>
                container.RegisterSingleton<INoClass>()).Output(output);
        }
        [Fact]
        public void Test05_Not_Assignable()
        {
            //Assert.Throws<ArgumentException>(() => 
            //    container.Register(typeof(IDisposable), typeof(SomeClass), null, Lifestyle.Singleton)).Output(output);
        }

        [Fact]
        public void Test06_Duplicate_Super()
        {
            container.RegisterSingleton<SomeClass>();
            Assert.Throws<TypeAccessException>(() =>
                container.RegisterSingleton<SomeClass>()).Output(output);
        }

        [Fact]
        public void Test07_Duplicate_Super_Interface()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<TypeAccessException>(() =>
                container.RegisterSingleton<ISomeClass, SomeClass>()).Output(output);

        }

        [Fact]
        public void Test08_Duplicate_Concrete()
        {
            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<TypeAccessException>(() =>
                container.RegisterSingleton<SomeClass>()).Output(output);
        }

    }
}
