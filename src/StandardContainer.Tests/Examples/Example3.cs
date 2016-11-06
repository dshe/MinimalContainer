using System;
using Xunit;

namespace StandardContainer.Tests.Examples
{
    internal interface IFoo2 { }
    internal class Foo2 : IFoo2 { }

    internal interface IFoo1
    {
        Func<IFoo2> Foo2Factory { get; }
    }

    internal class Foo1 : IFoo1
    {
        public Func<IFoo2> Foo2Factory { get; }
        internal Foo1(Func<IFoo2> foo2Factory)
        {
            Foo2Factory = foo2Factory;
        }
    }

    internal class Root
    {
        private readonly IFoo1 ifoo1;
        internal Root(IFoo1 ifoo1)
        {
            this.ifoo1 = ifoo1;
        }

        private void StartApplication()
        {
            var foo2 = ifoo1.Foo2Factory();
            Assert.IsType(typeof(Foo2), foo2);
        }

        public static void Main()
        {
            new Container(DefaultLifestyle.Transient)
                .Resolve<Root>()
                .StartApplication();
        }
    }
}
