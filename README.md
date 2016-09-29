
## StandardContainer&nbsp;&nbsp; [![release](https://img.shields.io/github/release/dshe/StandardContainer.svg)](https://github.com/dshe/StandardContainer/releases) [![status](https://ci.appveyor.com/api/projects/status/uuft89jhlm0xw22q/branch/master?svg=true)](https://ci.appveyor.com/project/dshe/standardcontainer/branch/master) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)

A simple and portable IoC (Inversion of Control) container.
- one C# 6.0 source file
- supports .NET Platform Standard 1.0
- supports public and internal constructor dependency injection
- supports automatic and/or explicit type registration
- supports transient and singleton (container) lifestyles
- supports enumerables and generics
- detects captive and recursive dependencies
- fluent interface
- fast

#### example
```csharp
public interface IFoo {}
public class Foo : IFoo {}

var container = new Container();

container.RegisterSingleton<IFoo, Foo>();

IFoo instance = container.GetInstance<IFoo>();

container.Dispose();
```
Disposing the container disposes any registered disposable singleton instances.

#### registration
```csharp
container.RegisterSingleton<Foo>();
container.RegisterSingleton<IFoo>();
container.RegisterSingleton<IFoo, Foo>();

container.RegisterTransient<Foo>();
container.RegisterTransient<IFoo>();
container.RegisterTransient<IFoo, Foo>();

container.RegisterInstance(Foo instance);
container.RegisterInstance<IFoo>(Foo instance);

container.RegisterFactory(() => new Foo());
container.RegisterFactory<IFoo>(() => new Foo());
```
#### resolution
```csharp
IFoo instance = container.GetInstance<IFoo>();
```
#### enumerables
```csharp
public class IFoo {}
public class Foo1 : IFoo {}
public class Foo2 : IFoo {}

var container = new Container();

container.RegisterSingleton<Foo1>();
container.RegisterSingleton<Foo2>();
container.RegisterSingleton<IEnumerable<IFoo>>();

IEnumerable<IFoo> enumerable = container.GetInstance<IEnumerable<Ifoo>>();
```
A list of instances of registered types which are assignable to `IFoo` is returned.
#### generics
```csharp
public class GenericParameterClass {}

public class GenericClass<T>
{
    public GenericClass(T t) {}
}

public class Foo
{
    public Foo(GenericClass<GenericParameterClass> g) {}
}

var container = new Container();

container.RegisterSingleton<GenericParameterClass>();
container.RegisterSingleton<GenericClass<GenericParameterClass>>();
container.RegisterSingleton<Foo>();

SomeClass instance = container.GetInstance<Foo>();
```
#### automatic registration
```csharp
public class Foo {}

var container = new Container(Lifestyle.Singleton, assemblies:someAssembly);

Foo instance = container.GetInstance<Foo>();
```
To enable automatic registration, pass the lifestyle to be used (singleton or transient) in the container's constructor. Note that the container will always register the dependencies of singleton instances as singletons.

If automatic type resolution requires scanning assemblies other than the current executing assembly, include references to those assemblies in the container's constructor.

#### example
```csharp
public interface IClassB {}
public class ClassB : IClassB {}

public class ClassC<T> { }
public class ClassD { }

public interface IClass {}
public class ClassE : IClass {}
public class ClassF : IClass {}

public class ClassA : IDisposable
{
    public ClassA(IClassB b, ClassC<ClassD> cd, IEnumerable<IClass> list) {}
    public void Dispose() {}
}

public class Root
{
    public Root(ClassA a)
    {
        StartApplication();
    }
}

using (var container = new Container(Lifestyle.Singleton))
    container.GetInstance<Root>();
```
The complete object graph is created and the application is started by simply resolving the compositional root. 
#### fluent examples
```csharp
var root = new Container(Lifestyle.Transient)
    .RegisterSingleton<T1>()
    .RegisterInstance(new T2())
    .RegisterFactory(() => new T3())
    .GetInstance<TRoot>();
```
```csharp
new Container(Lifestyle.Transient).GetInstance<TRoot>().StartApplication();
```
#### resolution strategy
The following graphic illustrates the automatic type resolution strategy:

![Image of Resolution Strategy](https://github.com/dshe/InternalContainer/blob/master/TypeResolutionFlowChart.png)


#### constructors
The container can create instances of types using public and internal constructors. In case a class has more than one constructor, the constructor to be used may be indicated by decorating it with the 'ContainerConstructor' attribute. Otherwise, the type is constructed using the constructor with the smallest number of arguments.
```csharp
public class Foo
{
    public Foo() {}

    [ContainerConstructor]    
    public Foo(Foo2 foo2) {}
}
```
#### logging
```csharp
var container = new Container(log:Console.WriteLine);
```
#### diagnostic
```csharp
foreach (var registration in container.Registrations())
  Debug.WriteLine(registration.ToString());
```
```csharp
Debug.WriteLine(container.ToString());
```
