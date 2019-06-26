using Xunit;
using MinimalContainer;

#nullable enable

namespace MinimalContainerTests
{
    public class Example1
    {
        public interface IFoo {}
        public class Foo : IFoo {}

        [Fact]
        public static void Mainx()
        {
            var container = new Container();
            container.RegisterSingleton<IFoo, Foo>();
            var foo = container.Resolve<IFoo>();
            // ...
        }
    }

}