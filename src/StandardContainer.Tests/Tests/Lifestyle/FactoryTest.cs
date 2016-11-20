using System;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Lifestyle
{
    public class FactoryTest : TestBase
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Func<SomeClass> factory1, factory2;
        private int counter1, counter2;

        public FactoryTest(ITestOutputHelper output) : base(output)
        {
            factory1 = () =>
            {
                counter1++;
                return new SomeClass();
            };
            factory2 = () =>
            {
                counter2++;
                return new SomeClass();
            };

        }

        [Fact]
        public void T01_Concrete()
        {
            var container = new Container(log: Write);
            container.RegisterFactory(factory1);
            Assert.Throws<TypeAccessException>(() => container.RegisterFactory(factory1)).Output(Write);
            var instance1 = container.Resolve<SomeClass>();
            Assert.Equal(1, counter1);
            var instance2 = container.Resolve<SomeClass>();
            Assert.Equal(2, counter1);
            Assert.NotEqual(instance1, instance2);
        }

        [Fact]
        public void T02_Register_Factory()
        {
            var container = new Container(log: Write);
            container.RegisterFactory<ISomeClass>(factory1);
            var instance1 = container.Resolve<ISomeClass>();
            Assert.Equal(1, counter1);
            var instance2 = container.Resolve<ISomeClass>();
            Assert.Equal(2, counter1);
            Assert.NotEqual(instance1, instance2);

        }

        [Fact]
        public void T03_Register_Factory_Both()
        {
            var container = new Container(log: Write);
            container.RegisterFactory(factory1);
            container.RegisterFactory<ISomeClass>(factory2);
            container.Resolve<SomeClass>();
            Assert.Equal(1, counter1);
            container.Resolve<ISomeClass>();
            Assert.Equal(1, counter2);
        }

        [Fact]
        public void T04_Register_Auto()
        {
            var container = new Container(log: Write, defaultLifestyle: DefaultLifestyle.Singleton);
            container.RegisterFactory(factory1);
            container.Resolve<SomeClass>();
            Assert.Equal(container.Resolve<ISomeClass>(), container.Resolve<ISomeClass>());
            Assert.Equal(1, counter1);
        }

    }
}
