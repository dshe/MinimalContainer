﻿using System;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
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

            foreach (var reg in container.Registrations())
                output.WriteLine(reg.ToString());

            container.Dispose();
        }

    }
}
