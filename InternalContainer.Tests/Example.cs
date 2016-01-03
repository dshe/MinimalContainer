using System;
using System.Reactive.Subjects;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class Example
    {
        public interface IClassA { }
        public class ClassA : IClassA { }
        private readonly ITestOutputHelper output;
        public Example(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test_Usage()
        {
            var subject = new Subject<string>();
            subject.Subscribe(output.WriteLine);

            var container = new Container(observer:subject);

            container.RegisterSingleton<IClassA, ClassA>();

            var instance = container.GetInstance<IClassA>();
            Assert.IsType<ClassA>(instance);
            Assert.Equal(instance, container.GetInstance<IClassA>());

            container.Dispose();
        }

    }
}
