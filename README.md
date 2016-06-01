# Akka.Interfaced

[![NuGet Status](http://img.shields.io/nuget/v/Akka.Interfaced.svg?style=flat)](https://www.nuget.org/packages/Akka.Interfaced/)
[![Build status](https://ci.appveyor.com/api/projects/status/ttuin5f31sj341n3?svg=true)](https://ci.appveyor.com/project/veblush/akka-interfaced)
[![Coverage Status](https://coveralls.io/repos/github/SaladLab/Akka.Interfaced/badge.svg?branch=master)](https://coveralls.io/github/SaladLab/Akka.Interfaced?branch=master)
[![Coverity Status](https://scan.coverity.com/projects/8460/badge.svg?flat=1)](https://scan.coverity.com/projects/saladlab-akka-interfaced)

Akka.Interfaced provides the type-safe and declarative way for actor communicating on Akka.NET.
This project is influenced by [WCF Contract](https://msdn.microsoft.com/en-us/library/ff183866.aspx) and
[Orleans](http://dotnet.github.io/orleans/).

## Example

For the first time, we need to design the interface of a greeting actor.
Greeting actor can greet and it can count how much it say hello.
Defining the interface `IGreeter` to show the way the actor communicate is natural for C# programmers.

```csharp
public interface IGreeter : IInterfacedActor
{
    Task<string> Greet(string name);
    Task<int> GetCount();
}
```

After defining interface, it's time to define the class implementing `IGreeter`, `GreetingActor`.
It should inherit `InterfacedActor` because it's an Akka.NET actor.
Implementing `IGreeter` interface is a bit simple.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    private int _count;

    Task<string> IGreeter.Greet(string name)
    {
        _count += 1;
        return Task.FromResult($"Hello {name}!");
    }

    Task<int> IGreeter.GetCount()
    {
        return Task.FromResult(_count);
    }
}
```

We designed interface and implemented the actor.
Finally we can send a message to and get a reply from `GreetingActor` actor by using `GreeterRef`.
`GreeterRef` implements `IGreeter`, you can get `IGreeter` instance from `GreeterRef` and use it as a regular C# interface.

```csharp
async Task TestAsync(ActorSystem system)
{
    // Create GreetingActor and make a reference pointing to an actor.
    var actor = system.ActorOf<GreetingActor>();
    var greeter = new GreeterRef(actor);

    // Make some noise
    Console.WriteLine(await greeter.Greet("World")); // Output: Hello World!
    Console.WriteLine(await greeter.Greet("Actor")); // Output: Hello Actor!
    Console.WriteLine(await greeter.GetCount());     // Output: 2
}
```

## Where can I get it?

Common projects using Akka.Interfaceds consist of at least two projects.
One is an interface project defining interfaces such as previous `IGreeter` and
the other is a main project defining the rest such as previous `GreetingActor`.

For an interface project:

```
PM> Install-Package Akka.Interfaced.Templates
```

For a main project:

```
PM> Install-Package Akka.Interfaced
```

For a detailed explanation, read [Project configuration](./Configuration.md).

## Manual

Comprehensive manual for using Akka.Interfaced: [Manual](./docs/Manual.md)
