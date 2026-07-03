namespace MinimalContainer.Examples;

public class Example1
{
    public interface IFoo {}
    public class Foo : IFoo {}

    [Fact]
    public static void Mainx()
    {
        Container container = new Container();
        container.RegisterSingleton<IFoo, Foo>();
        IFoo foo = container.Resolve<IFoo>();
        // ...
    }
}
