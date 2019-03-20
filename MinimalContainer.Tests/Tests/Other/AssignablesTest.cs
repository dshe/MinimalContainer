using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using MinimalContainer.Tests.Utility;
using Microsoft.Extensions.Logging;
using Divergic.Logging.Xunit;

namespace MinimalContainer.Tests.Other
{
    public class AssignablesTest : UnitTestBase
    {
        public AssignablesTest(ITestOutputHelper output) : base(output) { }

        internal interface INotUsed { }
        public interface IMarker {}
        public class SomeClass1 : IMarker {}
        public class SomeClass2 : IMarker {}
        public class SomeClass3
        {
            public IEnumerable<IMarker> List;
            public SomeClass3(IEnumerable<IMarker> list) => List = list;
        }

        [Fact]
        public void T00_Not_Registered()
        {
            var container = new Container(logger: Logger);
            Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<SomeClass1>>()).WriteMessageTo(Logger);
        }

        [Fact]
        public void T01_Registered()
        {
            var container = new Container(logger: Logger);
            container.RegisterSingleton<SomeClass1>();
            Assert.Single(container.Resolve<IList<SomeClass1>>());
        }

        [Fact]
        public void T02_DefaultLifestyle()
        {
            var container = new Container(DefaultLifestyle.Singleton, logger: Logger);
            Assert.Single(container.Resolve<IList<SomeClass1>>());
        }

        [Fact]
        public void T03_List()
        {
            var container = new Container(DefaultLifestyle.Singleton, logger: Logger);
            Assert.Single(container.Resolve<IList<SomeClass1>>());
            Assert.Equal(2, container.Resolve<IList<IMarker>>().Count());
        }

        [Fact]
        public void T04_List_Auto()
        {
            var container = new Container(logger: Logger, defaultLifestyle:DefaultLifestyle.Singleton);
            Assert.Single(container.Resolve<IList<SomeClass1>>());
            Assert.Equal(2, container.Resolve<IList<IMarker>>().Count());
        }

        [Fact]
        public void T05_Get_List_Types()
        {
            var container = new Container(logger: Logger, defaultLifestyle: DefaultLifestyle.Singleton);
            Assert.Equal(2, container.Resolve<IEnumerable<IMarker>>().Count());
            Assert.Equal(2, container.Resolve<ICollection<IMarker>>().Count);
            Assert.Equal(2, container.Resolve<IReadOnlyCollection<IMarker>>().Count);
            Assert.Equal(2, container.Resolve<IList<IMarker>>().Count);
            Assert.Equal(2, container.Resolve<IReadOnlyList<IMarker>>().Count);
            Assert.Throws<InvalidCastException>(() => container.Resolve<List<IMarker>>());
            Assert.Throws<InvalidOperationException>(() => container.Resolve<IMarker[]>());
        }

        [Fact]
        public void T06_Register_List()
        {
            var container = new Container(logger: Logger);
            var list = new List<SomeClass1> {new SomeClass1()};
            container.RegisterInstance(list);
            var instance = container.Resolve<List<SomeClass1>>();
            Assert.Single(instance);
        }

        [Fact]
        public void T07_Injection()
        {
            var container = new Container(logger: Logger);
            container.RegisterSingleton<SomeClass1>();
            container.RegisterSingleton<SomeClass2>();
            container.RegisterSingleton<SomeClass3>();
            container.RegisterSingleton<IList<IMarker>>();
            var instance = container.Resolve<SomeClass3>();
            Assert.Equal(2, instance.List.Count());
        }

        [Fact]
        public void T08_Injection_Auto()
        {
            var container = new Container(logger: Logger, defaultLifestyle: DefaultLifestyle.Singleton);
            var instance = container.Resolve<SomeClass3>();
            Assert.Equal(2, instance.List.Count());
        }

        [Fact]
        public void T09_Combo()
        {
            var container = new Container(logger: Logger, defaultLifestyle: DefaultLifestyle.Singleton);
            var instance = container.Resolve<Func<IList<IMarker>>>();
            Assert.Equal(2, instance().Count());
        }
    }
}
