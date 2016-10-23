using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Examples
{
    public interface IFoo2 {}
    public class Foo2 : IFoo2 {}

    public class Foo3<T> { }
    public class Foo4 { }

    public interface IClass {}
    public class Foo5 : IClass {}
    public class Foo6 : IClass {}

    public class Foo1
    {
        public Foo1(IFoo2 b, Foo3<Foo4> cd, IEnumerable<IClass> list) {}
    }

    public class Root
    {
        public Root(Foo1 a)
        {
            //Start();
        }
    }

    public class Main
    {
        private readonly Action<string> write;
        public Main(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void Start()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: write);
            container.GetInstance<Root>();
            container.Log();
        }


    }


}