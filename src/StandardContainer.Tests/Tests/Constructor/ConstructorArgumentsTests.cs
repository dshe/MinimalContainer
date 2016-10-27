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
            container = new Container(DefaultLifestyle.Singleton, log: write);
        }

        public class ClassWithValueTypeArgument
        {
            public ClassWithValueTypeArgument(int i) { }
        }
        [Fact]
        public void T01_Class_With_Value_Type_Argument()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassWithValueTypeArgument>()).Output(write);
        }

        public class ClassWithStringArgument
        {
            public ClassWithStringArgument(string s) { }
        }
        [Fact]
        public void T02_Class_With_String_Argument()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassWithStringArgument>()).Output(write);
        }

        public class ClassWithDefaultValueTypeArgument
        {
            public ClassWithDefaultValueTypeArgument(int i = 42) {}
        }
        [Fact]
        public void T03_Class_With_Default_Value_Type_Argument()
        {
            container.GetInstance<ClassWithDefaultValueTypeArgument>();
        }

        public class ClassWithDefaultStringArgument
        {
            public ClassWithDefaultStringArgument(string s = "test") {}
        }
        [Fact]
        public void T04_Class_With_Default_String_Argument()
        {
            container.GetInstance<ClassWithDefaultStringArgument>();
        }

        public class ClassWithNullArgument
        {
            public ClassWithNullArgument(string s = null) {}
        }
        [Fact]
        public void T05_Class_With_Null_Argument()
        {
            container.GetInstance<ClassWithNullArgument>();
        }

    }

}
