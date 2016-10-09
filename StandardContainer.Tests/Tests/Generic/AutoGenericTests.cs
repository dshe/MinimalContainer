using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Generic
{
    public class AutoGenericTests
    {
        internal interface IClassA {}
        internal class ClassA : IClassA {}
        internal class ClassB<T>
        {
            public ClassB(T t) {}
        }

        internal class ClassC
        {
            public ClassC(ClassB<ClassA> ba)
            { }
        }
        internal class ClassD
        {
            public ClassD(ClassB<IClassA> ba)
            { }
        }

        private readonly Container container;
        private readonly Action<string> write;

        public AutoGenericTests(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(DefaultLifestyle.Singleton, log:output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_01()
        {
            container.GetInstance<ClassC>();
            Assert.Equal(4, container.GetRegistrations().Count);
            write(Environment.NewLine + container);
        }

        [Fact]
        public void Test_02()
        {
            container.GetInstance<ClassD>();
            Assert.Equal(4, container.GetRegistrations().Count);
            write(Environment.NewLine + container);
        }


        public class Test
        {
            public Test(IObservable<object> obs) { }
        }

        public class ObsConcrete : IObservable<object>
        {
            public IDisposable Subscribe(IObserver<object> observer)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Test_03()
        {
            var b = container.GetInstance<ObsConcrete>();
            var x = container.GetInstance<Test>();
            write(Environment.NewLine + container);
        }
    }
}
