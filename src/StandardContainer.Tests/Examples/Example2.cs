using System;
using System.Collections.Generic;
using System.Reflection;
using StandardContainer;
using StandardContainer.Tests.Examples;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Examples
{
    public class Example2
    {
        internal interface IFoo1 {}
        internal interface IFoo2 {}
        internal class Foo1 : IFoo1 {}
        internal class Foo2 : IFoo2 {}
        internal class Root
        {
            private readonly IFoo1 foo1;
            private readonly Func<IFoo2> foo2Factory;
            internal Root(IFoo1 foo1, Func<IFoo2> foo2Factory)
            {
                this.foo1 = foo1;
                this.foo2Factory = foo2Factory;
            }
            private void StartApplication()
            {
                //...
            }
            public static void Main()
            {
                new Container(DefaultLifestyle.Transient)
                    .Resolve<Root>()
                    .StartApplication();
            }
        }
    }
}
