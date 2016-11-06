using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Examples
{
    public class Example1
    {
        public interface IFoo {}
        public class Foo : IFoo {}

        public static void Main()
        {
            var container = new Container();
            container.RegisterSingleton<IFoo, Foo>();
            IFoo foo = container.Resolve<IFoo>();
            // ...
        }
    }

}