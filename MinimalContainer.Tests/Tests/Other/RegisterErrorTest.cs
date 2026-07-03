namespace MinimalContainer.Tests.Other;

public class RegisterErrorTest : BaseUnitTest
{
    public interface INoClass { }
    public interface ISomeClass { }
    public class SomeClass : ISomeClass { }

    public RegisterErrorTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void T00_Various_types()
    {
        Container container = new(log: Log);
        Assert.Throws<ArgumentNullException>(() => container.RegisterInstance(typeof(object), null)).WriteMessageTo(Log);
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(int))).WriteMessageTo(Log);
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton(typeof(string))).WriteMessageTo(Log);
        Assert.Throws<TypeAccessException>(() => container.RegisterInstance(42)).WriteMessageTo(Log);
    }

    [Fact]
    public void T01_Abstract_No_Concrete()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<INoClass>();
        Assert.Throws<TypeAccessException>(() => container.Resolve<INoClass>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T02_Not_Assignable()
    {
        Container container = new(log: Log);
        container.RegisterSingleton(typeof(IDisposable), typeof(SomeClass));
        Assert.Throws<TypeAccessException>(() => container.Resolve<INoClass>()).WriteMessageTo(Log);
        Assert.Throws<TypeAccessException>(() => container.RegisterInstance(typeof(int), 42)).WriteMessageTo(Log);
    }

    [Fact]
    public void T03_Duplicate_Concrete()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<SomeClass>();
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T04_Duplicate_Interface()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<ISomeClass>();
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ISomeClass>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T05_Duplicate_Concrete_Interface()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<ISomeClass, SomeClass>();
        container.RegisterSingleton<SomeClass>();
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<SomeClass>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T05_Duplicate_Type()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<SomeClass>();
        Assert.Throws<TypeAccessException>(() => container.RegisterInstance(new SomeClass())).WriteMessageTo(Log); ;
    }

    [Fact]
    public void T06_Unregistered()
    {
        Container container = new(log: Log);
        Assert.Throws<TypeAccessException>(() => container.Resolve<SomeClass>()).WriteMessageTo(Log); ;
        Assert.Throws<TypeAccessException>(() => container.Resolve<ISomeClass>()).WriteMessageTo(Log); ;
        Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<ISomeClass>>()).WriteMessageTo(Log);
    }

}
