using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Examples
{
    public class Example0
    {
        public interface IFoo {}
        public class Foo : IFoo {}

        public void Main()
        {
            var container = new Container();

            container.RegisterSingleton<IFoo, Foo>();
            IFoo instance = container.GetInstance<IFoo>();
        }
    }

}