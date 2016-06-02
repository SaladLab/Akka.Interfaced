## Defining an actor class

To define an interfaced actor, write a class that inherits `InterfacedActor`.
Following `MyActor` is a new interfaced actor class that does nothing.

```csharp
public class MyActor : InterfacedActor
{
}
```

#### Basic interface

Making an interfaced actor class that implements interface `A` means an instance
of class can handle a request from calling a method of `A`.
For example, `GreetingActor` implements `IGreeter` like following and handles
`Greet` and `GetCount` requests.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    Task<string> IGreeter.Greet(string name) { ... }
    Task<int> IGreeter.GetCount() { ... }
}
```

This ensures that `GreetingActor` can handle `IGreeter` at compile-time and
protects you from making silly mistakes such as missing handler or mismatched signature.

An interfaced actor can implement multiple interfaces.
Following `DiningActor` can handle requests in `IGreeter` and `IServer`.

```csharp
public class DiningActor : InterfacedActor, IGreeter, IServer
{
    Task<string> IGreeter.Greet(string name) { ... }
    Task<int> IGreeter.GetCount() { ... }
    Task<int> IServer.Order(string name) { ... }
    // ...
}
```

#### Extended Interface

ExtendedHandler allows you to define an actor without explicit implementation.
Because an interfaced actor always uses async methods to provide handlers,
sometime it looks verbose and inefficient.   

```csharp
Task<int> IGreeter.GetCount()
{
    return Task.FromResult(_count);
}
```

Rather than this, following function is more simple and efficient.

```csharp
int IGreeter.GetCount()
{
    return _count;
}
```

But it is impossible to change the return type of interface
so `IExtendedInterface` is provided.

For example, GreetingActor can be defined as inheriting
`IExtendedInterface<IGreeter>` instead of `IGreeter` and it is not required to
define all methods `IGreeter`.

```csharp
public class GreetingActor : InterfacedActor, IExtendedInterface<IGreeter>
{
    // acts as IGreeter.Greet
    [ExtendedHandler] Task<string> Greet(string name) { ... }
    // acts as IGreeter.GetCount
    [ExtendedHandler] int GetCount() { return _count; }
}
```

ExtendedHandler should follow the signature of method but the return type of
method could be unwrapped. (Both of string and Task\<string\> are ok for returning string)

This cannot enforce an actor to follow compile-time interface contract, it can make
a runtime error which usually is caused in creating first actor of T.

Following class violates a contract and will make a runtime type error.

```csharp
public class GreetingActor : InterfacedActor, IExtendedInterface<IGreeter>
{
    // mismatched name
    [ExtendedHandler] Task<string> Hello(string name) { ... }
    // mismatched return type
    [ExtendedHandler] double GetCount() { ... }
}
```

If you want to ensure compile-time type-safety,
`Akka.Interfaced.CodeVerifier` can help you.
It scans all classes in a project and validates interface contracts.
For `GreetingActor` class, verifier reports following errors.

```
! Cannot find handler for HelloWorld.Interface.IGreeter.Greet
! Cannot find handler for HelloWorld.Interface.IGreeter.GetCount
```
