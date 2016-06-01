** WRITING IN PROCESS **

## Defining an actor class

Simple inheriting `IGreeter`

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    Task<string> IGreeter.Greet(string name) { ... }
    Task<int> IGreeter.GetCount() { ... }
}
```

Actor can inherit multiple interfaces.

ExtendedHandler allows you to define an actor without explicit inheriting.

```csharp
public class GreetingActor : InterfacedActor, IExtendedInterface<IGreeter>
{
    [ExtendedHandler] Task<string> Greet(string name) { ... }
    [ExtendedHandler] int GetCount() { ... }
}
```

Type-safety

```csharp
public class GreetingActor : InterfacedActor, IExtendedInterface<IGreeter>
{
    // [ExtendedHandler] Task<string> Greet(string name) { ... }
    [ExtendedHandler] double GetCount() { ... }
}
```

will got runtime error but CodeVerifier will help you on detecting type error in build time.

```
PM> Install-Package Akka.Interfaced.CodeVerifier
```

```
TODO: RESULT
```
