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
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }
        private readonly Container container;
        private readonly ITestOutputHelper output;

        public RegisterErrorTest(ITestOutputHelper output)
        {
            this.output = output;
            container = new Container(log: output.WriteLine,assembly:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test01_Null_SuperType()
        {
            Assert.Throws<ArgumentNullException>(() => 
                container.Register(null, typeof(SomeClass), () => new SomeClass(), Lifestyle.Singleton)).Output(output);
        }
        [Fact]
        public void Test02_Register_Error_Null()
        {
            Assert.Throws<ArgumentException>(() => 
                container.Register(typeof(ISomeClass), typeof(SomeClass), null, Lifestyle.AutoRegisterDisabled)).Output(output);
        }

        [Fact]
        public void Test03_Register_Error_Abstract()
        {
             container.RegisterSingleton<ISomeClass>();
        }

        [Fact]
        public void Test03_Register_Error_Duplicate()
        {
            container.RegisterTransient<SomeClass>();
            Assert.Throws<TypeAccessException>(() => 
                container.RegisterSingleton<SomeClass>()).Output(output);
            container.Dispose();

            container.RegisterSingleton<ISomeClass, SomeClass>();
            Assert.Throws<TypeAccessException>(() => 
                container.RegisterSingleton<ISomeClass, SomeClass>()).Output(output);
            Assert.Throws<TypeAccessException>(() => 
                container.RegisterSingleton<SomeClass>()).Output(output);

            //Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>());
            //Assert.Throws<TypeAccessException>(() => container.GetInstance<ISomeClass>());
        }



    }
}
