using System;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Other
{
    public class GenericTests : TestBase
    {
        public GenericTests(ITestOutputHelper output) : base(output) {}

        public class Foo2 {}
        public class Foo1<T>
        {
            public Foo1(T t) {}
        }
        public class Foo
        {
            public Foo(Foo1<Foo2> generic) {}
        }

        [Fact]
        public void T01_Generic()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<Foo2>();
            container.RegisterSingleton<Foo1<Foo2>>();
            container.RegisterSingleton<Foo>();
            container.Resolve<Foo>();
            Write(Environment.NewLine + container);
        }

        [Fact]
        public void T02_Generic_Auto()
        {
            var container = new Container(log: Write, defaultLifestyle:DefaultLifestyle.Singleton);
            container.Resolve<Foo>();
            Write(Environment.NewLine + container);
        }

        ////////////////////////////////////////////////////////////////////////////////

        internal interface IClassA { }
        internal class ClassA : IClassA { }
        internal class ClassB<T>
        {
            public ClassB(T t) {}
        }

        internal class ClassC
        {
            public ClassC(ClassB<ClassA> ba) {}
        }
        internal class ClassD
        {
            public ClassD(ClassB<IClassA> ba) {}
        }

        [Fact]
        public void T03_class()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            container.Resolve<ClassC>();
            Write(Environment.NewLine + container);
        }

        [Fact]
        public void T04_interface()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            container.Resolve<ClassD>();
            Write(Environment.NewLine + container);
        }

        ////////////////////////////////////////////////////////////////////////////////

        public class Test
        {
            public Test(IObservable<object> obs) {}
        }

        public class ObsConcrete : IObservable<object>
        {
            public IDisposable Subscribe(IObserver<object> observer)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void T05_parm()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            var b = container.Resolve<ObsConcrete>();
            Assert.Throws<NotImplementedException>(() => b.Subscribe(null));

            var x = container.Resolve<Test>();
            Write(Environment.NewLine + container);
        }

        ////////////////////////////////////////////////////////////////////////////////

        [Fact]
        public void T06_OpenGeneric()
        {
            Assert.True(typeof(Foo1<int>).IsGenericType);
            Assert.True(typeof(Foo1<>).IsGenericType);
            Assert.True(typeof(Foo1<>).IsGenericTypeDefinition);
            Assert.False(typeof(Foo1<int>).IsGenericTypeDefinition);
            Assert.Equal(typeof(Foo1<int>).GetGenericTypeDefinition(), typeof(Foo1<>));
        }

    }
}


