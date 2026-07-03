namespace MinimalContainer.Tests.Other;

public class AssignablesTest : BaseUnitTest
{
    public AssignablesTest(ITestOutputHelper output) : base(output) { }

    internal interface INotUsed { }
    public interface IMarker {}
    public class SomeClass1 : IMarker {}
    public class SomeClass2 : IMarker {}
    public class SomeClass3
    {
        public IEnumerable<IMarker> List;
        public SomeClass3(IEnumerable<IMarker> list) => List = list;
    }

    [Fact]
    public void T00_Not_Registered()
    {
        Container container = new(log: Log);
        Assert.Throws<TypeAccessException>(() => container.Resolve<IEnumerable<SomeClass1>>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T01_Registered()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<SomeClass1>();
        Assert.Single(container.Resolve<IList<SomeClass1>>());
    }

    [Fact]
    public void T02_DefaultLifestyle()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        Assert.Single(container.Resolve<IList<SomeClass1>>());
    }

    [Fact]
    public void T03_List()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        Assert.Single(container.Resolve<IList<SomeClass1>>());
        Assert.Equal(2, container.Resolve<IList<IMarker>>().Count());
    }

    [Fact]
    public void T04_List_Auto()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        Assert.Single(container.Resolve<IList<SomeClass1>>());
        Assert.Equal(2, container.Resolve<IList<IMarker>>().Count());
    }

    [Fact]
    public void T05_Get_List_Types()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        Assert.Equal(2, container.Resolve<IEnumerable<IMarker>>().Count());
        Assert.Equal(2, container.Resolve<ICollection<IMarker>>().Count);
        Assert.Equal(2, container.Resolve<IReadOnlyCollection<IMarker>>().Count);
        Assert.Equal(2, container.Resolve<IList<IMarker>>().Count);
        Assert.Equal(2, container.Resolve<IReadOnlyList<IMarker>>().Count);
        Assert.Throws<InvalidCastException>(() => container.Resolve<List<IMarker>>());
        Assert.Throws<InvalidOperationException>(() => container.Resolve<IMarker[]>());
    }

    [Fact]
    public void T06_Register_List()
    {
        Container container = new(log: Log);
        List<SomeClass1> list = [new SomeClass1()];
        container.RegisterInstance(list);
        List<SomeClass1> instance = container.Resolve<List<SomeClass1>>();
        Assert.Single(instance);
    }

    [Fact]
    public void T07_Injection()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<SomeClass1>();
        container.RegisterSingleton<SomeClass2>();
        container.RegisterSingleton<SomeClass3>();
        container.RegisterSingleton<IList<IMarker>>();
        SomeClass3 instance = container.Resolve<SomeClass3>();
        Assert.Equal(2, instance.List.Count());
    }

    [Fact]
    public void T08_Injection_Auto()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        SomeClass3 instance = container.Resolve<SomeClass3>();
        Assert.Equal(2, instance.List.Count());
    }

    [Fact]
    public void T09_Combo()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        Func<IList<IMarker>> instance = container.Resolve<Func<IList<IMarker>>>();
        Assert.Equal(2, instance().Count);
    }
}
