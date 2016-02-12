using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests.Generic
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
        private readonly ITestOutputHelper output;

        public AutoGenericTests(ITestOutputHelper output)
        {
            this.output = output;
            container = new Container(Lifestyle.Singleton, log:output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_01()
        {
            container.GetInstance<ClassC>();
            Assert.Equal(3, container.GetRegistrations().Count);
            output.WriteLine(Environment.NewLine + container);
        }

        [Fact]
        public void Test_02()
        {
            container.GetInstance<ClassD>();
            Assert.Equal(3, container.GetRegistrations().Count);
            output.WriteLine(Environment.NewLine + container);
        }



    }
}
