namespace MinimalContainer.Tests.TypeFactory;

public class TypeFactoryWithInterfaceTest : BaseUnitTest
{
    public interface IFoo { }
    public interface IBar { }

    public class Foo : IFoo { }
    public class Bar : IBar
    {
        public Bar(Func<IFoo> factory) { }
    }

    public TypeFactoryWithInterfaceTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void T01_transient_factory()
    {
        Container container = new();
        container.RegisterTransient<IFoo, Foo>();
        Func<IFoo> factory = container.Resolve<Func<IFoo>>();
        Assert.IsType<Foo>(factory());
        Assert.NotEqual(factory(), factory());
    }

    [Fact]
    public void T02_singleton_factory()
    {
        Container container = new();
        container.RegisterTransient<IFoo>();
        container.Resolve<Func<IFoo>>();
    }

    [Fact]
    public void T03_auto_singleton()
    {
        Container container = new(DefaultLifestyle.Singleton);
        container.Resolve<Func<IFoo>>();
    }

    [Fact]
    public void T04_auto_singleton_injection()
    {
        Container container = new(DefaultLifestyle.Singleton);
        container.Resolve<IBar>();
    }
}
