using System;
using System.Reactive.Subjects;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Relative
{
    public interface IMarker1 {}
    public interface IMarker2 {}

    public class ClassA1 : IMarker1, IMarker2 {}
    public class ClassA2 : IMarker1, IMarker2 { }
    public class ClassA3 : IMarker1, IMarker2 { }

    public class UniqueConcreteTest
    {
        private readonly Container container;

        public UniqueConcreteTest(ITestOutputHelper output)
        {
            var subject = new Subject<string>();
            subject.Subscribe(output.WriteLine);
            container = new Container(Lifestyle.Singleton, observer: subject);
        }

        [Fact]
        public void Test_DuplicateRegistration()
        {
            container.RegisterSingleton<ClassA1>();
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<ClassA1>());

            container.RegisterSingleton<IMarker1, ClassA2>();
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<IMarker1, ClassA2>());
        }
        [Fact]
        public void Test_RegistrationConcrete()
        {
            container.RegisterSingleton<ClassA1>();
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<IMarker1, ClassA1>());

            container.RegisterSingleton<IMarker1, ClassA2>();
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<ClassA2>());
            //Assert.Throws<ArgumentException>(() => container.RegisterSingleton<IMarker1, ClassA3>());
            Assert.Throws<ArgumentException>(() => container.RegisterSingleton<IMarker2, ClassA2>());
        }

        [Fact]
        public void Test_RegistrationConcreteMultiple()
        {
            container.RegisterSingleton<IMarker1,ClassA1>();
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassA1>());
            container.GetInstance<IMarker1>();
        }

    }

}
