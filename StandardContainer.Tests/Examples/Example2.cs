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
        internal interface IFoo2 {}
        internal class Foo2 : IFoo2 {}

        internal class Foo1
        {
            internal Foo1(IFoo2 foo2) {}
        }

        internal class Root
        {
            internal Root(Foo1 foo1) {}
            private void StartApplication()
            {
                //...
            }

            public static void Main()
            {
                new Container(DefaultLifestyle.Transient)
                    .GetInstance<Root>()
                    .StartApplication();
            }
        }
    }
}
