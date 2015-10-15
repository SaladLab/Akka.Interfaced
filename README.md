[![Build status](https://ci.appveyor.com/api/projects/status/ttuin5f31sj341n3?svg=true)](https://ci.appveyor.com/project/veblush/akka-interfaced)

** This project is at an early development stage. **

# Akka.Interfaced

Akka.Interfaced provides the interfaced way for actor messaging in Akka.NET

## HelloWorld Example

For the first time, we need to design the interface of HelloWorld actor.
HelloWorld actor can say hello and it can count how much it say hellow.
Defining the interface IHelloWorld to show the way the actor communicate is natural for C# programmers.

```csharp
public interface IHelloWorld : IInterfacedActor
{
	Task<string> SayHello(string name);
	Task<int> GetHelloCount();
}
```

After defining interface, it's time to define the class implementing IHelloWorld, HelloWorldActor.
It should inherit InterfacedActor\<HelloWorldActor\> because it's an Akka.NET actor. Implementing IHelloWorld interface is a bit simple.

```csharp
public class HelloWorldActor : InterfacedActor<HelloWorldActor>, IHelloWorld
{
	private int _helloCount;

	async Task<string> IHelloWorld.SayHello(string name)
	{
		await Task.Delay(100);
		_helloCount += 1;
		return string.Format("Hello {0}!", name);
	}

	Task<int> IHelloWorld.GetHelloCount()
	{
		return Task.FromResult(_helloCount);
	}
}
```

We designed interface and implemented the actor.
Finally we can send a message to and get a reply from HelloWorld actor by using HelloWorldRef.
HelloWorldRef implements IHelloWorld, you can get IHelloWorld instance from HelloWorldRef and use it as a regular C# interface.

```csharp
static void Test()
{
	var system = ActorSystem.Create("MySystem");
	DeadRequestProcessingActor.Install(system);

	// Create HelloWorldActor and get it's ref object
	var actor = system.ActorOf<HelloWorldActor>();
	var helloWorld = new HelloWorldRef(actor);

	// Make some noise
	Console.WriteLine(helloWorld.SayHello("World").Result);  // Hello World!
	Console.WriteLine(helloWorld.SayHello("Dlrow").Result);  // Hello Dlrow!
	Console.WriteLine(helloWorld.GetHelloCount().Result);    // 2
}
```

## Exception

When an actor process a message it can throw an exception and it propagate to caller.
TestActor.IncCounter will throw an ArgumentException if delta is not positive.

```csharp
public class TestActor : InterfacedActor<TestActor>, ICounter
{
	async Task ICounter.IncCounter(int delta)
	{
		if (delta <= 0)
			throw new ArgumentException("Delta should be positive");

		_counter += delta;
	}
	
	...
}
```

This time we call IncCounter with delta = -1. The actor will throw an exception and 
try-catch block in call-site will catch this exception.

```csharp
static async Task Test()
{
	var actor = System.ActorOf<TestActor>();
	var c = new CounterRef(actor);

	try
	{
		await c.IncCounter(-1);
	}
	catch (Exception e)
	{
		// 
		Console.WriteLine("! " + e);
	}
}
```

## Atomic and reentrant async handler

TODO

```csharp
public class TestActor : InterfacedActor<TestActor>, IWorker
{
	async Task IWorker.Atomic(string name)
	{
		Console.WriteLine("Atomic({0}) Enter", name);
		await Task.Delay(10);
		Console.WriteLine("Atomic({0}) Mid", name);
		await Task.Delay(10);
		Console.WriteLine("Atomic({0}) Leave", name);
	}

	[Reentrant]
	async Task IWorker.Reentrant(string name)
	{
		Console.WriteLine("Reentrant({0}) Enter", name);
		await Task.Delay(10);
		Console.WriteLine("Reentrant({0}) Mid", name);
		await Task.Delay(10);
		Console.WriteLine("Reentrant({0}) Leave", name);
	}
}
```

```csharp
static async Task Test(IActorRef actor)
{
	var w = new WorkerRef(actor);

	await Task.WhenAll(
		w.Atomic("A"),
		w.Atomic("B"));

	await Task.WhenAll(
		w.Reentrant("A"),
		w.Reentrant("B"));
}
```

## InterfacedObserver

TODO

## Protobuf-net

TODO

## CodeGeneration

TODO

## Message Handler Decorator

TODO

## Ask without temporary actor

TODO

## SlimClient

TODO
