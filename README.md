## InternalContainer.cs
A simple IOC container in a single C# 6.0 source file.
- **no dependencies**
- **portable** library compatibility: Windows 10, Framework 4.6, ASP.NET Core 5
- supports constructor dependency injection
- selects the public or **internal** constructor with the most arguments
- supports singleton and transient lifestyles
- supports Generics
- detects captive and recursive dependencies
- fast enough

#### example
```csharp
public class TSuper {}
public class TConcrete : TSuper {}

var container = new Container();

container.RegisterSingleton<TSuper,TConcrete>();

var instance = container.GetInstance<TSuper>();

container.Dispose();
```
`TSuper` is a superType of `TConcrete`. Often an interface, it could also be an abstract class or possibly a concrete type which is assignable from `TConcrete`.  

Disposing the container will dispose any registered disposable singleton instances.

#### registration of single types
```csharp
container.RegisterSingleton<TConcrete>();
container.RegisterSingleton<TSuper,TConcrete>();
container.RegisterInstance(new TConcrete());
container.RegisterInstance<TSuper>(new TConcrete());

container.RegisterTransient<TConcrete>();
container.RegisterTransient<TSuper,TConcrete>();
container.RegisterFactory(() => new TConcrete());
container.RegisterFactory<TSuper>(() => new TConcrete());
```
#### registration of enumerable types
```csharp
container.RegisterSingleton<IEnumerable<TSuper>>();
container.RegisterTransient<IEnumerable<TSuper>>();
```
#### resolution of a single type
```csharp
T instance = container.GetInstance<T>();
T instance = (T)container.GetInstance(typeof(T));
```
```csharp
var container = new Container();

container.RegisterSingleton<TSuper,TConcrete>();
TSuper instance = container.GetInstance<TSuper>();
Assert.Equal(instance, container.GetInstance<TSuper>());

container.RegisterInstance<TSuper>(new TConcrete());
TSuper instance = container.GetInstance<TSuper>();
Assert.Equal(instance, container.GetInstance<TSuper>());

container.RegisterTransient<TSuper,TConcrete>();
TSuper instance = container.GetInstance<TSuper>();
Assert.NotEqual(instance, container.GetInstance<TSuper>());

container.RegisterFactory<TSuper>(() => new TConcrete());
TSuper instance = container.GetInstance<TSuper>();
Assert.NotEqual(instance, container.GetInstance<TSuper>());
```
#### resolution of multiple types
```csharp
IEnumerable<TSuper> instances = container.GetInstance<IEnumerable<TSuper>>();
```
```csharp
public class TSuper {}
public class TConcrete1 : TSuper {}
public class TConcrete2 : TSuper {}

var container = new Container();

container.RegisterSingleton<TSuper,TConcrete1>();
container.RegisterSingleton<TSuper,TConcrete2>();

IEnumerable<TSuper> instances = container.GetInstance<IEnumerable<TSuper>>();
Assert.Equal(2, instances.Count);
```
A list of instances of registered types which are assignable to `TSuper` is returned.

#### automatic registration
```csharp
var container = new Container(Lifestyle.Singleton);
//container.RegisterSingleton<TConcrete>();
TConcrete instance = container.GetInstance<TConcrete>();
Assert.Equal(instance, container.GetInstance<TConcrete>());
```
To enable automatic registration and resolution, pass the desired lifestyle (singleton or transient) to be used for automatic registration in the container's constructor. If type resolution requires scanning assemblies other than the current executing assembly, those assemblies can also be passed in the continer's constructor.

The following graphic illustrates the strategy used to automatically resolve types:

![Image of Resolution Strategy](https://github.com/dshe/InternalContainer/blob/master/InternalContainer/TypeResolutionFlowChart.png)

#### example
```csharp
public interface IClassA {}
public class ClassA : IClassA {}

public interface IClass {}
public class ClassB : IClass {}
public class ClassC : IClass {}

public class ClassD<T> {}
public class ClassE {}

public class ClassB : IDisposable
{
  public ClassB(IClassA a, IEnumerable<IClass> list, ClassD<ClassE> de) {}
  public void Dispose() {}
}

public class Root
{
  public Root(ClassB b) 
  {
    Start();
  }
}

using (var container = new Container(Lifestyle.Singleton))
  container.GetInstance<Root>();
```
In the example above, the complete object graph is created and the application started by simply resolving the compositional root. 

#### logging
```csharp
var container = new Container(log:Console.WriteLine);
```
#### diagnostic
```csharp
foreach (var registration in container.Registrations())
  Debug.WriteLine(registration.ToString());
```
