using System;
using System.Reactive.Subjects;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class RegisterAllTest
    {
        private readonly Container container;

        public RegisterAllTest(ITestOutputHelper output)
        {
            var subject = new Subject<string>();
            subject.Subscribe(output.WriteLine);
            container = new Container(observer: subject, assembly:Assembly.GetExecutingAssembly());
        }

        public interface INotUsed { }
        public interface ISomeClass { }
        public class SomeClass1 : ISomeClass { }
        public class SomeClass2 : ISomeClass { }

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
