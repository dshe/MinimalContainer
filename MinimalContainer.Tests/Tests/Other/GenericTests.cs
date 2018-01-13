using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Other
{
    public class GenericTests1
    {
        public class Bar2 { }
        public class Bar1<T>
        {
            public Bar1(T t) { }
        }
        public class Foo
        {
            public Foo(Bar1<Bar2> generic) { }
        }

        private readonly Action<string> Write;
        public GenericTests1(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void T01_Generic()
        {
            var container = new Container(log: Write);
            container.RegisterSingleton<Bar2>();
            container.RegisterSingleton<Bar1<Bar2>>();
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

        [Fact]
        public void T03_OpenGeneric()
        {
            Assert.True(typeof(Bar1<int>).GetTypeInfo().IsGenericType);
            Assert.True(typeof(Bar1<>).GetTypeInfo().IsGenericType);
            Assert.True(typeof(Bar1<>).GetTypeInfo().IsGenericTypeDefinition);
            Assert.False(typeof(Bar1<int>).GetTypeInfo().IsGenericTypeDefinition);
            Assert.Equal(typeof(Bar1<>), typeof(Bar1<int>).GetGenericTypeDefinition());
        }

    }

    public class GenericTests2
    {
        internal interface IClassA { }
        internal class ClassA : IClassA { }
        internal class ClassB<T>
        {
            public ClassB(T t) { }
        }

        internal class ClassC
        {
            public ClassC(ClassB<ClassA> ba) { }
        }

        internal class ClassD
        {
            public ClassD(ClassB<IClassA> ba) { }
        }

        private readonly Action<string> Write;
        public GenericTests2(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void T01_class()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            container.Resolve<ClassC>();
            Write(Environment.NewLine + container);
        }

        [Fact]
        public void T02_interface()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            container.Resolve<ClassD>();
            Write(Environment.NewLine + container);
        }

        ////////////////////////////////////////////////////////////////////////////////

        public class Test
        {
            public Test(IObservable<object> obs) { }
        }

        public class ObsConcrete : IObservable<object>
        {
            public IDisposable Subscribe(IObserver<object> observer)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void T10_parm()
        {
            var container = new Container(DefaultLifestyle.Singleton, Write);
            var b = container.Resolve<ObsConcrete>();
            Assert.Throws<NotImplementedException>(() => b.Subscribe(null));

            var x = container.Resolve<Test>();
            Write(Environment.NewLine + container);
        }
    }

}


