using System;
using System.Collections.Generic;
using System.Reflection;
using InternalContainer;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainerTests.Examples
{
    public interface IClassB {}
    public class ClassB : IClassB {}

    public class ClassC<T> { }
    public class ClassD { }

    public interface IClass {}
    public class ClassE : IClass {}
    public class ClassF : IClass {}

    public class ClassA : IDisposable
    {
        public ClassA(IClassB b, ClassC<ClassD> cd, IEnumerable<IClass> list) {}
        public void Dispose() {}
    }

    public class Root
    {
        public Root(ClassA a)
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
            using (var container = new Container(Container.Lifestyle.Singleton, log: write, assemblies:Assembly.GetExecutingAssembly()))
            {
                container.GetInstance<Root>();
                container.Log();
            }
        }
    }
}