using System;
using System.Collections.Generic;
using System.Linq;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Enumerable
{
    public class GetEnumerableTest : TestBase
    {
        internal interface INotUsed { }
        public interface IMarker {}
        public class SomeClass1 : IMarker {}
        public class SomeClass2 : IMarker {}
        public class SomeClass3
        {
            public IEnumerable<IMarker> List;
            public SomeClass3(IEnumerable<IMarker> list)
            {
                List = list;
            }
        }

        public GetEnumerableTest(ITestOutputHelper output) : base(output) {}

        [Fact]
        public void T00_Not_Registered()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<SomeClass1>>()).WriteMessageTo(Write);
        }

        [Fact]
        public void T01_Registered()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass1>();
            Assert.Equal(1, container.Resolve<IEnumerable<SomeClass1>>().Count());
        }

        [Fact]
        public void T02_DefaultLifestyle()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            Assert.Equal(1, container.Resolve<IEnumerable<SomeClass1>>().Count());
        }

        [Fact]
        public void T03_Enumerable()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            Assert.Equal(1, container.Resolve<IEnumerable<SomeClass1>>().Count());
            Assert.Equal(2, container.Resolve<IEnumerable<IMarker>>().Count());
        }

        [Fact]
        public void T04_Enumerable_Auto()
        {
            var container = new Container(log: Write, defaultLifestyle:DefaultLifestyle.Singleton);
            Assert.Equal(1, container.Resolve<IEnumerable<SomeClass1>>().Count());
            Assert.Equal(2, container.Resolve<IEnumerable<IMarker>>().Count());
        }

        [Fact]
        public void T05_Get_Enumerable_Types()
        {
            var container = new Container(log: Write, defaultLifestyle: DefaultLifestyle.Singleton);
            Assert.Equal(2, container.Resolve<IEnumerable<IMarker>>().Count());
            Assert.Equal(2, container.Resolve<IList<IMarker>>().Count);
            Assert.Equal(2, container.Resolve<ICollection<IMarker>>().Count);
            Assert.Equal(2, container.Resolve<IReadOnlyCollection<IMarker>>().Count);
            Assert.Equal(2, container.Resolve<IReadOnlyList<IMarker>>().Count);
            //Assert.Equal(2, container.Resolve<List<IMarker>>().Count);
        }

        [Fact]
        public void T06_Register_Enumerable()
        {
            var container = new Container(log: Write);
            var list = new List<SomeClass1> {new SomeClass1()};
            container.RegisterInstance(list);
            var instance = container.Resolve<List<SomeClass1>>();
            Assert.Equal(1, instance.Count());
        }

        [Fact]
        public void T07_Injection()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterSingleton<SomeClass3>();
            container.RegisterSingleton<IEnumerable<IMarker>>();
            var instance = container.Resolve<SomeClass3>();
            Assert.Equal(2, instance.List.Count());
        }

        [Fact]
        public void T08_Injection_auto()
        {
            var container = new Container(log: Write, defaultLifestyle: DefaultLifestyle.Singleton);
            var instance = container.Resolve<SomeClass3>();
            Assert.Equal(2, instance.List.Count());
        }

    }
}
