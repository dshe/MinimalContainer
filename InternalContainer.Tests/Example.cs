using System;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class SomeData
    {
        public static ITestOutputHelper Output;
        public string ss;
        public SomeData(ITestOutputHelper output)
        {
            Output = output;
        }
    }

    public static class Extensions
    {
        public static SomeData Xx(this SomeData s, string str)
        {
            SomeData.Output.WriteLine(str);
            //return default(SomeData);
            return null;
        }

    }


    public class Example
    {
        public interface IClassA { }
        public class ClassA : IClassA { }
        private readonly ITestOutputHelper output;
        public Example(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test_Usage()
        {
            var container = new Container(Lifestyle.Singleton, log:output.WriteLine);

            container.RegisterSingleton<IClassA, ClassA>();

            var instance = container.GetInstance<IClassA>();
            Assert.IsType<ClassA>(instance);
            Assert.Equal(instance, container.GetInstance<IClassA>());

            foreach (var map in container.Maps())
                output.WriteLine(map.ToString());

            container.Dispose();
        }

        [Fact]
        public void Fuid()
        {
            var s = "";

            var y = new SomeData(output).Xx("a").Xx("b");

            if (y == null)
                output.WriteLine("y is null!");

        }

    }
}
