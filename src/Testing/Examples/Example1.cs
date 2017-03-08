using StandardContainer;

namespace Testing.Examples
{
    public class Example1
    {
        public interface IFoo {}
        public class Foo : IFoo {}

        public static void Mainx()
        {
            var container = new Container();
            container.RegisterSingleton<IFoo, Foo>();
            var foo = container.Resolve<IFoo>();
            // ...
        }
    }

}