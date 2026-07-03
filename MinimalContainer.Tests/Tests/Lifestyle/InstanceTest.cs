namespace MinimalContainer.Tests.Lifestyle;

public class InstanceTest : BaseUnitTest
{
    public interface IFoo { }
    public class Foo : IFoo { }

    public InstanceTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void T01_Concrete()
    {
        Container container = new(log: Log);
        Foo instance = new();
        container.RegisterInstance(instance);
        Foo instance1 = container.Resolve<Foo>();
        Assert.Equal(instance, instance1);
        Foo instance2 = container.Resolve<Foo>();
        Assert.Equal(instance1, instance2);
        Assert.Throws<TypeAccessException>(container.Resolve<IFoo>).WriteMessageTo(Log);
        Assert.Throws<TypeAccessException>(() => container.RegisterInstance(instance)).WriteMessageTo(Log);
    }

    [Fact]
    public void T02_Interface()
    {
        Container container = new(log: Log);
        Foo instance = new();
        container.RegisterInstance<IFoo>(instance);
        IFoo instance1 = container.Resolve<IFoo>();
        Assert.Equal(instance, instance1);
        IFoo instance2 = container.Resolve<IFoo>();
        Assert.Equal(instance1, instance2);
        Assert.Throws<TypeAccessException>(() => container.Resolve<Foo>());
        Assert.Throws<TypeAccessException>(() => container.RegisterInstance<IFoo>(instance)).WriteMessageTo(Log);
    }

}
