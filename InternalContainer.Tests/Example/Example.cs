using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Example
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
        private readonly ITestOutputHelper output;
        public Main(ITestOutputHelper output)
        {
            this.output = output;
        }
        [Fact]
        public void Start()
        {
            using (var container = new Container(Lifestyle.Singleton, log:output.WriteLine, assemblies:Assembly.GetExecutingAssembly()))
            {
                container.GetInstance<Root>();
                container.Log();
            }
        }
    }
}