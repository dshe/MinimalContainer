namespace MinimalContainer.Tests.Relative;

public class UniqueConcreteTest : BaseUnitTest
{
    public interface IMarker1 { }
    public interface IMarker2 { }

    public class ClassA : IMarker1, IMarker2 { }
    public class ClassB : IMarker1, IMarker2 { }
    public class ClassC : IMarker1, IMarker2 { }

    public UniqueConcreteTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void T01_Duplicate_Registration()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);

        container.RegisterSingleton<ClassA>();
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<ClassA>()).WriteMessageTo(Log);

        container.RegisterSingleton<IMarker1, ClassB>();
        Assert.Throws<TypeAccessException>(() => container.RegisterSingleton<IMarker1, ClassB>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T02_Registration_Duplicate_Marker()
    {
        Container container = new(DefaultLifestyle.Singleton, Log);
        Assert.Throws<TypeAccessException>(() => container.Resolve<IMarker1>()).WriteMessageTo(Log);
    }

    [Fact]
    public void T03_Registration_Concrete_Multiple()
    {
        Container container = new(log: Log);
        container.RegisterSingleton<IMarker1, ClassA>();
        container.Resolve<IMarker1>();
        Assert.Throws<TypeAccessException>(() => container.Resolve<ClassA>()).WriteMessageTo(Log);
    }
}
