namespace MinimalContainer.Tests.Other;

public class DisposeTest : BaseUnitTest
{
    public class Foo : IDisposable
    {
        public bool IsDisposed;
        public void Dispose() => IsDisposed = true;
    }

    public DisposeTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void T01_Dispose_Singleton()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        container.RegisterSingleton<Foo>();
        Foo instance = container.Resolve<Foo>();
        container.Dispose();
        Assert.True(instance.IsDisposed);
    }

    [Fact]
    public void T02_Dispose_Instance()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        Foo instance = new();
        container.RegisterInstance(instance);
        container.Dispose();
        Assert.True(instance.IsDisposed);
    }

    [Fact]
    public void T03_Dispose_Other()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        container.RegisterTransient<Foo>();
        Foo instance = container.Resolve<Foo>();
        container.Dispose();
        Assert.False(instance.IsDisposed);

        container.RegisterFactory(() => instance);
        container.Dispose();
        Assert.False(instance.IsDisposed);
    }

}
