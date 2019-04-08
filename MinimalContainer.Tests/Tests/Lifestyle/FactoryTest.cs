using System;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;
using Microsoft.Extensions.Logging;
using Divergic.Logging.Xunit;

namespace MinimalContainer.Tests.Lifestyle
{
    public class FactoryTest : BaseUnitTest
    {
        public interface ISomeClass { }
        public class SomeClass : ISomeClass { }

        private readonly Func<SomeClass> _factory1, _factory2;
        private int _counter1, _counter2;

        public FactoryTest(ITestOutputHelper output) : base(output)
        {
            _factory1 = () =>
            {
                _counter1++;
                return new SomeClass();
            };
            _factory2 = () =>
            {
                _counter2++;
                return new SomeClass();
            };

        }

        [Fact]
        public void T01_Concrete()
        {
            var container = new Container(logger: Logger);
            container.RegisterFactory(_factory1);
            Assert.Throws<TypeAccessException>(() => container.RegisterFactory(_factory1)).WriteMessageTo(Logger);
            var instance1 = container.Resolve<SomeClass>();
            Assert.Equal(1, _counter1);
            var instance2 = container.Resolve<SomeClass>();
            Assert.Equal(2, _counter1);
            Assert.NotEqual(instance1, instance2);
        }

        [Fact]
        public void T02_Register_Factory()
        {
            var container = new Container(logger: Logger);
            container.RegisterFactory<ISomeClass>(_factory1);
            var instance1 = container.Resolve<ISomeClass>();
            Assert.Equal(1, _counter1);
            var instance2 = container.Resolve<ISomeClass>();
            Assert.Equal(2, _counter1);
            Assert.NotEqual(instance1, instance2);

        }

        [Fact]
        public void T03_Register_Factory_Both()
        {
            var container = new Container(logger: Logger);
            container.RegisterFactory(_factory1);
            container.RegisterFactory<ISomeClass>(_factory2);
            container.Resolve<SomeClass>();
            Assert.Equal(1, _counter1);
            container.Resolve<ISomeClass>();
            Assert.Equal(1, _counter2);
        }

        [Fact]
        public void T04_Register_Auto()
        {
            var container = new Container(logger: Logger, defaultLifestyle: DefaultLifestyle.Singleton);
            container.RegisterFactory(_factory1);
            container.Resolve<SomeClass>();
            Assert.Equal(container.Resolve<ISomeClass>(), container.Resolve<ISomeClass>());
            Assert.Equal(1, _counter1);
        }

    }
}
