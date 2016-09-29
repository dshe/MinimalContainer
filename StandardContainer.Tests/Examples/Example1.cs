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

    public class Foo1 : IDisposable
    {
        public Foo1(IFoo2 b, Foo3<Foo4> cd, IEnumerable<IClass> list) {}
        public void Dispose() {}
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
            using (var container = new Container(Container.Lifestyle.Singleton, log:write, assemblies:Assembly.GetExecutingAssembly()))
            {
                container.GetInstance<Root>();
                container.Log();
            }
        }
    }
}