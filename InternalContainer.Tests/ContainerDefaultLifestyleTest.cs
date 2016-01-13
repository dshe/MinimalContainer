using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InternalContainer.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainer.Tests
{
    public class ContainerDefaultLifestyleTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly ITestOutputHelper output;
        public ContainerDefaultLifestyleTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test_Unregistered()
        {
            var c = new Container(log:output.WriteLine);
            Assert.Throws<TypeAccessException>(() => c.GetInstance<SomeClass>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<ISomeClass>());
            Assert.Throws<TypeAccessException>(() => c.GetInstance<IEnumerable<ISomeClass>>()).Output(output);
        }
        [Fact]
        public void Test_Singleton()
        {
            var c = new Container(Lifestyle.Singleton, log: output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
            var instance1 = c.GetInstance<SomeClass>();
            var reg = c.Registrations().Single();
            Assert.Equal(Lifestyle.Singleton, reg.Lifestyle);
            Assert.Equal(1, reg.Instances);
            var instance2 = c.GetInstance<SomeClass>();
            Assert.Equal(instance1, instance2);
            Assert.Equal(1, reg.Instances);
        }
        [Fact]
        public void Test_Transient()
        {
            var c = new Container(Lifestyle.Transient, log: output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
            var instance1 = c.GetInstance<SomeClass>();
            var reg = c.Registrations().Single();
            Assert.Equal(Lifestyle.Transient, reg.Lifestyle);
            Assert.Equal(1, reg.Instances);
            var instance2 = c.GetInstance<SomeClass>();
            Assert.NotEqual(instance1, instance2);
            Assert.Equal(2, reg.Instances);
        }
    }
}
