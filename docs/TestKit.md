## TestKit

Writing tests are essential to keep code using actors robust.
But it is challenging to write test code with actors because they are
inherently asynchronous and therefore not deterministic.

Akka.NET provides [TestKit](https://www.nuget.org/packages/Akka.TestKit/) to handle those difficulties. It provides a simple way to write deterministic test code and many utilities that help you.

If you are new to test code on Akka.NET, read [How to Unit Test Akka.NET Actors with Akka.TestKit](https://petabridge.com/blog/how-to-unit-test-akkadotnet-actors-akka-testkit/).

#### Test Code

Writing test code with interfaced actors is not that different with regular ones.

```csharp
var greeter = new GreeterRef(system.ActorOf<GreetingActor>());
Assert.Equal("Hello World!", await greeter.Greet("World"));
Assert.Equal(1, await greeter.GetCount());
```

But writing test code with `ActorBoundSession` is not simple so
`Akka.Interfaced.TestKit` is provided.

#### TestActorBoundSession

ActorBoundSession is a base class for implementing the gateway for slim client.
But setup for slim client and service actor and inspection are not easy.
To solve this problem, `Akka.Interfaced.TestKit` is provided.

Following code arranges a test client and `TestActorBoundSession` and
tests the user login process.

```csharp
// Arrange (Create session and bind UserLogin actor to it)
TestActorBoundSession session = ...;

// Act (Ask UserLogin actor to login and get User actor reference)
var userLogin = session.CreateRef<UserLoginRef>();
var observer = session.CreateObserver<IUserObserver>(null);
var user = await userLogin.Login("userid", "password", observer);

// Assert
Assert.Equal("userid", await user.GetId());
```
