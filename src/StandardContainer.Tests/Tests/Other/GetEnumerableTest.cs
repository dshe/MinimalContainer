using System;
using System.Collections.Generic;
using System.Linq;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
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
        public void T00_Get_No_Types()
        {
            var container = new Container(log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<INotUsed>>()).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<SomeClass1>>()).Output(Write);
        }

        [Fact]
        public void T01_Enumerable()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass1>();
            Assert.Equal(1, container.Resolve<IEnumerable<SomeClass1>>().Count());
            Assert.Equal(1, container.Resolve<IEnumerable<IMarker>>().Count());
            container.RegisterSingleton<SomeClass2>();
            var list = container.Resolve<IEnumerable<IMarker>>();
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public void T02_Enumerable_Auto()
        {
            var container = new Container(log: Write, defaultLifestyle:DefaultLifestyle.Singleton);
            Assert.Equal(1, container.Resolve<IEnumerable<SomeClass1>>().Count());
            Assert.Equal(2, container.Resolve<IEnumerable<IMarker>>().Count());
        }

        [Fact]
        public void T03_Get_Enumerable_Types()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            var list = container.Resolve<IEnumerable<IMarker>>();
            Assert.Equal(2, list.Count());
            list = container.Resolve<IList<IMarker>>();
            Assert.Equal(2, list.Count());
            list = container.Resolve<ICollection<IMarker>>();
            Assert.Equal(2, list.Count());
            list = container.Resolve<IReadOnlyCollection<IMarker>>();
            Assert.Equal(2, list.Count());
            list = container.Resolve<IReadOnlyList<IMarker>>();
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public void T04_Register_Enumerable()
        {
            var container = new Container(log: Write);
            var list = new List<SomeClass1>();
            container.RegisterInstance<IEnumerable<SomeClass1>>(list);
            var instance = container.Resolve<IEnumerable<SomeClass1>>();
            Assert.Equal(list, instance);
        }

        [Fact]
        public void T05_Injection()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterSingleton<SomeClass3>();
            var instance = container.Resolve<SomeClass3>();
            Assert.Equal(2, instance.List.Count());
        }

        [Fact]
        public void T06_Injection_auto()
        {
            var container = new Container(log: Write, defaultLifestyle: DefaultLifestyle.Singleton);
            var instance = container.Resolve<SomeClass3>();
            Assert.Equal(2, instance.List.Count());
        }

    }
}
