
## MinimalContainer&nbsp;&nbsp; [![release](https://img.shields.io/github/release/dshe/StandardContainer.svg)](https://github.com/dshe/StandardContainer/releases) [![Build status] [![Build status](https://ci.appveyor.com/api/projects/status/xig5mmbk9lqus99h?svg=true)](https://ci.appveyor.com/project/dshe/minimalcontainer) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)

***A minimal single file IoC (Inversion of Control) container.***
- automatic and/or explicit type registration
- public and **internal** constructor injection
- injection of instances, type factories and collections
- transient and singleton lifestyles
- captive and recursive dependency detection
- contained in a **single** C# 7.2 source file supporting **.NET Standard 2.0**
- fluent interface
- tested
- fast

#### example
```csharp
public interface IFoo {}
public class Foo : IFoo {}

public static void Main()
{
    Container container = new Container();
    container.RegisterTransient<IFoo, Foo>();
    IFoo foo = container.Resolve<IFoo>();
    ...
```
#### registration
```csharp
container.RegisterSingleton<Foo>();
container.RegisterSingleton<IFoo, Foo>();

container.RegisterTransient<Foo>();
container.RegisterTransient<IFoo, Foo>();

container.RegisterInstance(new Foo());
container.RegisterInstance<IFoo>(new Foo());

container.RegisterFactory(() => new Foo());
container.RegisterFactory<IFoo>(() => new Foo());
```
#### type resolution
```csharp
T instance = container.Resolve<T>();
```
#### resolution of assignable types
```csharp
IList<T> instances = container.Resolve<IList<T>>();
```
A list of instances of registered types which are assignable to `T` is returned.
#### resolution of type factories
```csharp
Func<T> factory = container.Resolve<Func<T>>();
T instance = factory();
```
#### constructors
The container can create instances of types using public and internal constructors. In case a type has more than one constructor, decorate the constructor to be used with the 'ContainerConstructor' attribute. Otherwise, the constructor with the smallest number of arguments is selected.
```csharp
public class Foo
{
    public Foo() {}

    [ContainerConstructor]
    public Foo(IBar bar) {}
}
```
#### automatic registration
```csharp
public class T {}

var container = new Container(DefaultLifestyle.Singleton);

T instance = container.Resolve<T>();
```
To enable automatic registration, set the default lifestyle to singleton or transient when constructing the container. Note that the container will always register the dependencies of singleton instances as singletons. If automatic type resolution requires scanning assemblies other than the assembly where the container is created, include references to those assemblies in the container's constructor.
#### fluency
```csharp
Foo1 foo1 = new Container()
    .RegisterSingleton<Foo1>()
    .RegisterTransient<Foo2>()
    .RegisterInstance(new Foo3())
    .RegisterFactory(() => new Foo4())
    .Resolve<Foo1>();
```
#### example
```csharp
internal interface IFoo {}
internal interface IBar {}
internal class Foo : IFoo {}
internal class Bar : IBar {}

internal class Root
{
    private readonly IFoo foo;
    private readonly Func<IBar> barFactory;

    internal Root(IFoo foo, Func<IBar> barFactory)
    {
        this.foo = foo;
        this.barFactory = barFactory;
    }

    private void StartApplication()
    {
        //...
    }
    
    public static void Main()
    {
        new Container(DefaultLifestyle.Singleton)
            .Resolve<Root>()
            .StartApplication();
    }
}
```
The complete object graph is created by simply resolving the compositional root. 
#### disposal
```csharp
container.Dispose();
```
Disposing the container disposes any registered disposable singletons.
#### logging
```csharp
var container = new Container(log:Debug.WriteLine);
```
#### diagnostic
```csharp
Debug.WriteLine(container.ToString());
```
