using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class EnumerableTest
    {
        private readonly Container container;

        public EnumerableTest(ITestOutputHelper output)
        {
            var subject = new Subject<string>();
            subject.Subscribe(output.WriteLine);
            container = new Container(Lifestyle.Singleton, observer: subject, assembly:Assembly.GetExecutingAssembly());
        }

        public interface IMarker {}
        public class ClassA : IMarker {}
        public class ClassB : IMarker {}

        [Fact]
        public void Test_Register_Enumerable()
        {
            container.RegisterSingleton<IMarker, ClassA>();
            container.RegisterSingleton<IMarker, ClassB>();

            Assert.Throws<TypeAccessException>(() => container.GetInstance<IMarker>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassA>());
        }

        [Fact]
        public void Test_RegisterAll_Enumerable()
        {
            container.RegisterAll(typeof(IMarker), Lifestyle.Singleton);
            var list = container.GetInstance<IEnumerable<IMarker>>();
            Assert.Equal(2, list.Count());
        }

    }
}
