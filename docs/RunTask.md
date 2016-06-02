## RunTask

An actor can send a message to itself, which is useful to schedule
a job out of current execution context.

Following `MyActor` schedules executing `Handle(SomeCustomMessage)` in executing `Handle(string)`.

```csharp
public class MyActor : InterfacedActor
{
    [MessageHandler] void Handle(string message)
    {
        // executing Handle(SomeCustomMessage) is scheduled
        Self.Tell(new SomeCustomMessage(...));
    }

    [MessageHandler] void Handle(SomeCustomMessage message) { ... }
}
```

You can schedule executing an anonymous method by `RunTask`.

```csharp
public class MyActor : InterfacedActor
{
    [MessageHandler] void Handle(string message)
    {
         // executing the anonymous method is scheduled
         RunTask(() => { ... });
    }
}
```

`RunTask` also can schedule running an asynchronous method with a reentrant option.

```csharp
class InterfacedActor
{
    void RunTask(Action action);
    void RunTask(Func<Task> function);
    void RunTask(Func<Task> function, bool isReentrant);
}
```
