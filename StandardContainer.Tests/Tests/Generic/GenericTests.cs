using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Generic
{
    public class GenericTests
    {
        internal class GenericParameterClass { }
        internal class GenericClass<T>
        {
            public GenericClass(T t) { }
        }

        internal class SomeClass
        {
            public SomeClass(GenericClass<GenericParameterClass> generic) { }
        }

        private readonly Container container;
        private readonly Action<string> write;

        public GenericTests(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(log: write, assemblies: Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_01()
        {
            container.RegisterSingleton<GenericParameterClass>();
            container.RegisterSingleton<GenericClass<GenericParameterClass>>();
            container.RegisterSingleton<SomeClass>();

            container.GetInstance<SomeClass>();
            Assert.Equal(4, container.GetRegistrations().Count);
            write(Environment.NewLine + container);
        }

        [Fact]
        public void Test_OpenGeneric()
        {
            //container.RegisterSingleton(typeof(GenericClass<>));
            //container.GetInstance(typeof(GenericClass<>));
            //write(Environment.NewLine + container);
        }
    }
}
