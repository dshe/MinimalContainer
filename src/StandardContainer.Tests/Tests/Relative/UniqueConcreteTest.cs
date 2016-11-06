using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public class UniqueConcreteTest
    {
        private readonly Action<string> write;

        public UniqueConcreteTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }


        public interface IMarker1 {}
        public interface IMarker2 {}

        public class ClassA1 : IMarker1, IMarker2 {}
        public class ClassA2 : IMarker1, IMarker2 {}
        public class ClassA3 : IMarker1, IMarker2 {}


        [Fact]
        public void T01_Duplicate_Registration()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: write);

            container.RegisterSingleton<ClassA1>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ClassA1>()).Output(write);

            container.RegisterSingleton<IMarker1, ClassA2>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker1, ClassA2>()).Output(write);
        }

        [Fact]
        public void T02_Registration_Duplicate_Marker()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IMarker1>()).Output(write);
        }

        [Fact]
        public void T03_Registration_Concrete_Multiple()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: write);
            container.RegisterSingleton<IMarker1,ClassA1>();
            container.Resolve<ClassA1>();
            container.Resolve<IMarker1>();
        }

    }

}
