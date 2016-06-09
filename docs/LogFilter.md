## LogFilter

LogFilter is a filter writing logs of request, notification and message.
It's quite useful for understanding how actor works and debugging.

For example, following actor class annotated with `[Log]` will show you
what is going on the actor.

```csharp
[Log, ResponsiveException(typeof(ArgumentException))]
class GreetingActor : InterfacedActor, IGreeterSync
{
    static Logger s_logger = LogManager.GetCurrentClassLogger();
    int _count;

    string IGreeterSync.Greet(string name) { ... }
    int IGreeterSync.GetCount() { ... }

    [MessageHandler] void OnMessage(string message) { ... }
}
```

You can see `[Log]` attribute on the top of class and `s_logger` which LogFilter
will write log messages to.

It captures input and output of request.

```csharp
await greeter.Greet("World");
// Log: <- (#-1) Greet {"name":"World"}
// Log: -> (#-1) Greet "Hello World!"
```

When a responsive exception is thrown, it will be shown as a result.

```csharp
await greeter.Greet(null);
// Log: <- (#-1) Greet {}
// Log: -> (#-1) Greet Exception: System.ArgumentException: name
```

Also you can see incoming messages but outgoing ones are not possible to see.

```csharp
greeter.Actor.Tell("Bye!");
// Log: <- OnMessage String("Bye!")
```

#### Setup

To use log filter to specific actor class, you need to setup two things:

- Add `[Log]` to a class or methods which should be traced.
- Make a class contain a `Logger` field or property that can write logs.

For example, previous `GreetingActor` has `[Log]` attribute of class and
logger instance, `s_logger`.

```csharp
[Log]
class GreetingActor : InterfacedActor
{
    static Logger s_logger = LogManager.GetCurrentClassLogger();
}
```

LogFilter finds a logger instance whose name looks like `*logger` or `*log*`
automatically but you can specify exact logger name like following.

```csharp
[Log(loggerName="_tracer")]
class GreetingActor : InterfacedActor
{
    Logger _tracer;
}
```

#### Log driver

It's not easy to maintain supports for many log drivers.
So LogFilter uses a reflection to support various log drivers such as log4net, NLog, etc.
If logger instance provides following methods, it can be used for log target.

- bool Is{LogLevel}Enabled
- void {LogLevel}(string/object message)

`LogLevel` is `Trace` by default and configurable.
