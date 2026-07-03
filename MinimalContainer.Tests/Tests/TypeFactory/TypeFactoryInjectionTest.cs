namespace MinimalContainer.Tests.TypeFactory;

public class TypeFactoryInjectionTest : BaseUnitTest
{
    public class Foo { }

    public class Bar
    {
        public readonly Func<Foo> Factory;
        public Bar(Func<Foo> factory) => Factory = factory;
    }

    public TypeFactoryInjectionTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void T00_injection()
    {
        Container container = new();
        container.RegisterTransient<Bar>();
        container.RegisterTransient<Foo>();

        Bar bar = container.Resolve<Bar>();
        Assert.NotEqual(bar.Factory(), bar.Factory());
    }

    [Fact]
    public void T01_auto_singleton_injection()
    {
        Container container = new(DefaultLifestyle.Singleton);
        Bar bar = container.Resolve<Bar>();
        Assert.NotEqual(bar.Factory(), bar.Factory());
    }

    [Fact]
    public void T02_auto_transient_injection()
    {
        Container container = new(DefaultLifestyle.Transient);
        Bar bar = container.Resolve<Bar>();
        Assert.NotEqual(bar.Factory(), bar.Factory());
    }

    [Fact]
    public void T03_injection()
    {
        Container container = new();
        container.RegisterTransient<Bar>();
        container.RegisterSingleton<Foo>();
        Assert.Throws<TypeAccessException>(() => container.Resolve<Bar>());
    }

}
