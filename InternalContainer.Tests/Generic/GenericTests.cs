using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Generic
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
            public SomeClass(GenericClass<GenericParameterClass> generic)
            { }
        }

        private readonly Container container;
        private readonly ITestOutputHelper output;

        public GenericTests(ITestOutputHelper output)
        {
            this.output = output;
            container = new Container(log: output.WriteLine, assemblies: Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_01()
        {
            container.RegisterSingleton<GenericParameterClass>();
            container.RegisterSingleton<GenericClass<GenericParameterClass>>();
            container.RegisterSingleton<SomeClass>();

            container.GetInstance<SomeClass>();
            Assert.Equal(3, container.GetRegistrations().Count);
            output.WriteLine(Environment.NewLine + container);
        }

        [Fact]
        public void Test_OpenGeneric()
        {
            //container.RegisterType(typeof(GenericClass<>), null, Lifestyle.Singleton);
            //container.GetInstance(typeof(GenericClass<>));
            //output.WriteLine(Environment.NewLine + container);
        }
    }
}
