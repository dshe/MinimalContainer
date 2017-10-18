using System;

namespace StandardContainer.Tests.Examples
{
    public class Example2
    {
        internal interface IFoo {}
        internal interface IBar {}

        internal class Foo : IFoo {}
        internal class Bar : IBar {}

        internal class Root
        {
            private readonly IFoo _foo;
            private readonly Func<IBar> _barFactory;
            internal Root(IFoo foo, Func<IBar> barFactory)
            {
                _foo = foo;
                _barFactory = barFactory;
            }

            private void StartApplication()
            {
                //...
            }

            public static void Mainx()
            {
                new Container(DefaultLifestyle.Transient)
                    .Resolve<Root>()
                    .StartApplication();
            }
        }
    }
}
