using System;
using Xunit;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.TypeFactory
{
    public class TypeFactoryInjectionTest
    {
        public class Foo { }

        public class Bar
        {
            public readonly Func<Foo> Factory;
            public Bar(Func<Foo> factory) => Factory = factory;
        }

        private readonly Action<string> Write;
        public TypeFactoryInjectionTest(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public void T00_injection()
        {
            var container = new Container();
            container.RegisterTransient<Bar>();
            container.RegisterTransient<Foo>();

            var bar = container.Resolve<Bar>();
            Assert.NotEqual(bar.Factory(), bar.Factory());
        }

        [Fact]
        public void T01_auto_singleton_injection()
        {
            var container = new Container(DefaultLifestyle.Singleton);
            var bar = container.Resolve<Bar>();
            Assert.NotEqual(bar.Factory(), bar.Factory());
        }

        [Fact]
        public void T02_auto_transient_injection()
        {
            var container = new Container(DefaultLifestyle.Transient);
            var bar = container.Resolve<Bar>();
            Assert.NotEqual(bar.Factory(), bar.Factory());
        }

        [Fact]
        public void T03_injection()
        {
            var container = new Container();
            container.RegisterTransient<Bar>();
            container.RegisterSingleton<Foo>();
            Assert.Throws<TypeAccessException>(() => container.Resolve<Bar>());
        }

    }
}
