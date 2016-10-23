using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Core
{
    public class GetEnumerableTest
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

        private readonly Action<string> write;
        public GetEnumerableTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        [Fact]
        public void T00_Get_No_Types()
        {
            var container = new Container(log: write);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<IEnumerable<INotUsed>>()).Output(write);
        }

        [Fact]
        public void T01_Enumerable()
        {
            var container = new Container(log: write);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<IEnumerable<SomeClass1>>()).Output(write);
            container.RegisterSingleton<SomeClass1>();
            Assert.Equal(1, container.GetInstance<IEnumerable<SomeClass1>>().Count());
            Assert.Equal(1, container.GetInstance<IEnumerable<IMarker>>().Count());
            container.RegisterSingleton<SomeClass2>();
            Assert.Equal(2, container.GetInstance<IEnumerable<IMarker>>().Count());
        }

        [Fact]
        public void T02_Enumerable_Auto()
        {
            var container = new Container(log: write, defaultLifestyle:DefaultLifestyle.Singleton);
            Assert.Equal(1, container.GetInstance<IEnumerable<SomeClass1>>().Count());
            Assert.Equal(2, container.GetInstance<IEnumerable<IMarker>>().Count());
        }

        [Fact]
        public void T03_Get_Enumerable_Types()
        {
            var container = new Container(log: write);
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            var list = container.GetInstance<IEnumerable<IMarker>>();
            Assert.Equal(2, list.Count());
            list = container.GetInstance<IList<IMarker>>();
            Assert.Equal(2, list.Count());
            list = container.GetInstance<ICollection<IMarker>>();
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public void T04_Register_Enumerable()
        {
            var container = new Container(log: write);
            var list = new List<SomeClass1>();
            container.RegisterInstance<IEnumerable<SomeClass1>>(list);
            var instance = container.GetInstance<IEnumerable<SomeClass1>>();
            Assert.Equal(list, instance);
        }

        [Fact]
        public void T05_Injection()
        {
            var container = new Container(log: write);
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterSingleton<SomeClass3>();
            var instance = container.GetInstance<SomeClass3>();
            Assert.Equal(2, instance.List.Count());
        }

        [Fact]
        public void T06_Injection_auto()
        {
            var container = new Container(log: write, defaultLifestyle: DefaultLifestyle.Singleton);
            var instance = container.GetInstance<SomeClass3>();
            Assert.Equal(2, instance.List.Count());
        }

    }
}
