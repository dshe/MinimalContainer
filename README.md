## InternalContainer.cs
A simple IoC (Inversion of Control) container.
- one C# 6.0 source file with no dependencies
- portable class library (PCL) compatibility: at least Windows Universal 10, .Net Framework 4.6, ASP.NET Core 5
- supports constructor dependency injection (selects the public or internal constructor with the most arguments)
- supports automatic or explicit type registration
- supports transient and singleton (container) lifestyles
- supports enumerables and closed generics
- detects captive and recursive dependencies
- tested
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
`TSuper` is a superType of `TConcrete`. Often an interface, it could also be an abstract class or possibly a concrete type which is assignable from `TConcrete`.  

Disposing the container will dispose any registered disposable singleton instances.

#### type registration
```csharp
container.RegisterSingleton<T>();
container.RegisterSingleton(typeof(T));
container.RegisterSingleton<TSuper, TConcrete>();
container.RegisterSingleton(typeof(TSuper), typeof(TConcrete);

container.RegisterTransient<T>();
container.RegisterTransient(typeof(T));
container.RegisterTransient<TSuper, TConcrete>();
container.RegisterTransient(typeof(TSuper), typeof(TConcrete);

container.RegisterInstance(new TConcrete());
container.RegisterInstance<TSuper>(new TConcrete());

container.RegisterFactory(() => new TConcrete());
container.RegisterFactory<TSuper>(() => new TConcrete());
```
#### type resolution
```csharp
T instance = container.GetInstance<T>();
T instance = (T)container.GetInstance(typeof(T));
```
#### enumerable types
```csharp
public class TSuper {}
public class TConcrete1 : TSuper {}
public class TConcrete2 : TSuper {}

var container = new Container();

container.RegisterSingleton<TConcrete1>>();
container.RegisterSingleton<TConcrete2>>();
container.RegisterSingleton<IEnumerable<TSuper>>();

IEnumerable<TSuper> enumerable = container.GetInstance<IEnumerable<TSuper>>();
```
A list of instances of registered types which are assignable to `TSuper` is returned.
#### generic types
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
#### automatic type registration and resolution
```csharp
public class TConcrete {}

var container = new Container(Lifestyle.Singleton, assemblies:someAssembly);

TConcrete instance = container.GetInstance<TConcrete>();
```
To enable automatic registration and resolution, pass the desired lifestyle (singleton or transient) to be used for automatic registration in the container's constructor. Note however that the container will always register the dependencies of singleton instances as singletons.

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
        Start();
    }
}

using (var container = new Container(Lifestyle.Singleton))
    container.GetInstance<Root>();
```
The complete object graph is created and the application is started by simply resolving the compositional root. 

#### type resolution strategy
The following graphic illustrates the automatic type resolution strategy:

![Image of Resolution Strategy](https://github.com/dshe/InternalContainer/blob/master/TypeResolutionFlowChart.png)

#### logging
```csharp
var container = new Container(log:Console.WriteLine);
```
#### diagnostic
```csharp
foreach (var registration in container.Registrations())
  Debug.WriteLine(registration.ToString());
```
