
## StandardContainer [![Build status](https://ci.appveyor.com/api/projects/status/uuft89jhlm0xw22q/branch/master?svg=true)](https://ci.appveyor.com/project/dshe/standardcontainer/branch/master) [![release](https://img.shields.io/github/release/dshe/StandardContainer.svg)](https://github.com/dshe/StandardContainer/releases) [![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

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
public class TSuper {}
public class TConcrete : TSuper {}

var container = new Container();

container.RegisterSingleton<TSuper,TConcrete>();

TSuper instance = container.GetInstance<TSuper>();

container.Dispose();
```
`TSuper`, usually an interface, is a superType of `TConcrete`. 

Disposing the container will dispose any registered disposable singleton instances.

#### registration
```csharp
container.RegisterSingleton<T>();
container.RegisterSingleton(typeof(T));
container.RegisterSingleton<TSuper, TConcrete>();
container.RegisterSingleton(typeof(TSuper), typeof(TConcrete));

container.RegisterTransient<T>();
container.RegisterTransient(typeof(T));
container.RegisterTransient<TSuper, TConcrete>();
container.RegisterTransient(typeof(TSuper), typeof(TConcrete));

container.RegisterInstance(new TConcrete());
container.RegisterInstance<TSuper>(new TConcrete());

container.RegisterFactory(() => new TConcrete());
container.RegisterFactory<TSuper>(() => new TConcrete());
```
#### resolution
```csharp
T instance = container.GetInstance<T>();
T instance = (T)container.GetInstance(typeof(T));
```
#### enumerables
```csharp
public class TSuper {}
public class TConcrete1 : TSuper {}
public class TConcrete2 : TSuper {}

var container = new Container();

container.RegisterSingleton<TConcrete1>();
container.RegisterSingleton<TConcrete2>();
container.RegisterSingleton<IEnumerable<TSuper>>();

IEnumerable<TSuper> enumerable = container.GetInstance<IEnumerable<TSuper>>();
```
A list of instances of registered types which are assignable to `TSuper` is returned.
#### generics
```csharp
public class GenericParameterClass {}

public class GenericClass<T>
{
    public GenericClass(T t) {}
}

public class SomeClass
{
    public SomeClass(GenericClass<GenericParameterClass> g) {}
}

var container = new Container();

container.RegisterSingleton<GenericParameterClass>();
container.RegisterSingleton<GenericClass<GenericParameterClass>>();
container.RegisterSingleton<SomeClass>();

SomeClass instance = container.GetInstance<SomeClass>();
```
#### automatic registration
```csharp
public class TConcrete {}

var container = new Container(Lifestyle.Singleton, assemblies:someAssembly);

TConcrete instance = container.GetInstance<TConcrete>();
```
To enable automatic registration, pass the desired lifestyle (singleton or transient) to be used for automatic registration in the container's constructor. Note however that the container will always register the dependencies of singleton instances as singletons.

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
public class ClassA
{
    public ClassA() {}

    [ContainerConstructor]    
    public ClassA(int i) {}
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
