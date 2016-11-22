using System;
using System.Reflection;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public class UniqueConcreteTest : TestBase
    {
        public UniqueConcreteTest(ITestOutputHelper output) : base(output) {}

        public interface IMarker1 {}
        public interface IMarker2 {}

        public class ClassA1 : IMarker1, IMarker2 {}
        public class ClassA2 : IMarker1, IMarker2 {}
        public class ClassA3 : IMarker1, IMarker2 {}

        [Fact]
        public void T01_Duplicate_Registration()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);

            container.RegisterSingleton<ClassA1>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ClassA1>()).Output(Write);

            container.RegisterSingleton<IMarker1, ClassA2>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker1, ClassA2>()).Output(Write);
        }

        [Fact]
        public void T02_Registration_Duplicate_Marker()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IMarker1>()).Output(Write);
        }

        [Fact]
        public void T03_Registration_Concrete_Multiple()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<IMarker1, ClassA1>();
            container.Resolve<IMarker1>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassA1>()).Output(Write);
        }

    }

}
