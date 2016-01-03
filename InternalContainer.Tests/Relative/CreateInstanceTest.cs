using System;
using System.Reactive.Subjects;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Relative
{
    public class ClassX
    {
        public ClassX(ClassY y) { }
    }

    public class ClassY
    {
        public ClassY(ClassZ z) { }
    }

    public class ClassZ
    {
        public ClassZ(int x) { }
    }

    public class CreateInstanceTest
    {
        private readonly Container container;

        public CreateInstanceTest(ITestOutputHelper output)
        {
            var subject = new Subject<string>();
            subject.Subscribe(output.WriteLine);
            container = new Container(Lifestyle.Singleton, observer: subject);
        }

        [Fact]
        public void Test_Cannot_Create_Dependency()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassZ>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassY>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassX>());
        }
    }
}
