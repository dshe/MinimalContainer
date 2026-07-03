namespace MinimalContainer.Tests.Lifestyle;

public class SingletonTest : BaseUnitTest
{
    public interface IFoo { }
    public class Foo : IFoo { }

    public SingletonTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void T01_Concrete()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<Foo>();
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<Foo>()).WriteMessageTo(Log);
        Assert.Equal(container.Resolve<Foo>(), container.Resolve<Foo>());
        Assert.Throws<TypeAccessException>(() => container.Resolve<IFoo>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T02_Interface()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<IFoo>();
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IFoo>()).WriteMessageTo(Log);
        Assert.Equal(container.Resolve<IFoo>(), container.Resolve<IFoo>());
        Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T03_Concrete_Interface()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<IFoo>();
        container.RegisterSingleton<Foo>();
        Assert.NotEqual(container.Resolve<Foo>(), container.Resolve<IFoo>());
    }

    [Fact]
    public void T04_Register_Singleton()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<IFoo, Foo>();
        Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>()).WriteMessageTo(Log);
        container.RegisterSingleton<Foo>();
        Assert.NotEqual(container.Resolve<IFoo>(), container.Resolve<Foo>());
    }

    [Fact]
    public void T05_Register_Singleton_Auto()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        Assert.NotEqual(container.Resolve<IFoo>(), container.Resolve<Foo>());
    }

}
