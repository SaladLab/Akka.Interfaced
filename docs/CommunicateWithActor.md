** WRITING IN PROCESS **

## Communicating with an actor

### How to use

```csharp
IActorRef actor = CreateOrGetActorRef<GreetingActor>();
var greeter = new GreeterRef(actor);
```

```csharp
var hello = await greeter.Greet("Actor");
var count = await greeter.GetCount();
```

### RequestWaitor

- Default one is based on akka Ask method.
- In interfaced actor, it's better that RequestWaitor is set to `this`.
  - No temporary actor for waiting response Message

### Timeout

- Actor whom you sent request could be busy or terminated.
- To handle this situation, timeout helps.

```csharp
greeter.WithTimeout(TimeSpan.FromSeconds(3)).Greet("Actor");
```

### WithNoReply

- Whenever you request to actor, you wait for response.
- For void return type, waiting is necessary because it notices request is successfully handled without an exception.
- But sometime, you want fire-and-forget.

```csharp
greeter.WithNoReply().Greet("Actor");
```
