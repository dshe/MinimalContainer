using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;

namespace MinimalContainer.Tests.Relative
{
    public class UniqueConcreteTest
    {
        public interface IMarker1 { }
        public interface IMarker2 { }

        public class ClassA : IMarker1, IMarker2 { }
        public class ClassB : IMarker1, IMarker2 { }
        public class ClassC : IMarker1, IMarker2 { }

        private readonly Action<string> Write;
        public UniqueConcreteTest(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void T01_Duplicate_Registration()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);

            container.RegisterSingleton<ClassA>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ClassA>()).WriteMessageTo(Write);

            container.RegisterSingleton<IMarker1, ClassB>();
            Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker1, ClassB>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T02_Registration_Duplicate_Marker()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IMarker1>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T03_Registration_Concrete_Multiple()
        {
            var container = new Container(logAction: Write);
            container.RegisterSingleton<IMarker1, ClassA>();
            container.Resolve<IMarker1>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<ClassA>()).WriteMessageTo(Write);
        }
    }
}
