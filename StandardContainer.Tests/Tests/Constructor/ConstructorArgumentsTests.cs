using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Constructor
{
    public class ConstructorArgumentTests
    {
        private readonly Container container;
        private readonly Action<string> write;

        public ConstructorArgumentTests(ITestOutputHelper output)
        {
            write = output.WriteLine;
            container = new Container(autoLifestyle: Container.Lifestyle.Singleton, log: write, assemblies: Assembly.GetExecutingAssembly());
        }

        public class ClassWithValueTypeArgument
        {
            public ClassWithValueTypeArgument(int i) { }
        }
        [Fact]
        public void Test_ClassWithValueTypeArgument()
        {
            container.RegisterSingleton<ClassWithValueTypeArgument>();
            var ex = Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassWithValueTypeArgument>());
            write(ex.Message);
        }

        public class ClassWithDefaultValueTypeArgument
        {
            public int I;
            public ClassWithDefaultValueTypeArgument(int i = 42) { I = i; }
        }
        [Fact]
        public void Test_ClassWithDefaultValueTypeArgument()
        {
            container.RegisterSingleton<ClassWithDefaultValueTypeArgument>();
            var x = container.GetInstance<ClassWithDefaultValueTypeArgument>();
            Assert.Equal(42, x.I);
        }

        public class ClassWithStringArgument
        {
            public ClassWithStringArgument(string s) { }
        }
        [Fact]
        public void Test_ClassWithStringArgument()
        {
            container.RegisterSingleton<ClassWithStringArgument>();
            var ex = Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassWithStringArgument>());
            write(ex.Message);
        }

        public class ClassWithDefaultStringArgument
        {
            public string S;
            public ClassWithDefaultStringArgument(string s = "test") { S = s; }
        }
        [Fact]
        public void Test_ClassWithDefaultStringArgument()
        {
            container.RegisterSingleton<ClassWithDefaultStringArgument>();
            var x = container.GetInstance<ClassWithDefaultStringArgument>();
            Assert.Equal("test", x.S);
        }

        public class ClassWithNullArgument
        {
            public ClassWithNullArgument(string s = null) { }
        }
        [Fact]
        public void Test_ClassWithNullArgument()
        {
            container.RegisterSingleton<ClassWithNullArgument>();
            container.GetInstance<ClassWithNullArgument>();
        }

    }

}
