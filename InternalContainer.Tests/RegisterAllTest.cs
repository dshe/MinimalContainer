using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class RegisterAllTest
    {
        public interface INotUsed { }
        public interface ISomeClass { }
        public class SomeClass1 : ISomeClass { }
        public class SomeClass2 : ISomeClass { }
        private readonly Container container;

        public RegisterAllTest(ITestOutputHelper output)
        {
            container = new Container(log: output.WriteLine, assembly:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Register_Error_Null()
        {
            Assert.Throws<ArgumentNullException>(() => container.RegisterAll(null, Lifestyle.Singleton));
        }

        [Fact]
        public void Test_Register_Error_Abstract()
        {
            //Assert.Throws<ArgumentException>(() => container.RegisterSingletonAll<SomeClass1>());
            Assert.Throws<ArgumentException>(() => container.RegisterSingletonAll<INotUsed>());
            container.RegisterAll(typeof(ISomeClass), Lifestyle.Singleton);
        }

    }
}
