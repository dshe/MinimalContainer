using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Generics
{
    public class GenericTests
    {
        private readonly Container container;
        private readonly Action<string> write;

        public GenericTests(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(log: write);
        }

        internal class Foo2 { }
        internal class Foo1<T>
        {
            public Foo1(T t) { }
        }

        internal class Foo
        {
            public Foo(Foo1<Foo2> generic) { }
        }

        [Fact]
        public void Test_01()
        {
            container.RegisterSingleton<Foo2>();
            container.RegisterSingleton<Foo1<Foo2>>();
            container.RegisterSingleton<Foo>();

            container.GetInstance<Foo>();
            write(Environment.NewLine + container);
        }

        [Fact]
        public void Test_OpenGeneric()
        {
            //container.RegisterSingleton(typeof(Foo1<>));
            //container.GetInstance(typeof(Foo1<>));
            //write(Environment.NewLine + container);
        }

    }
}
