# 해야 할 일

## Actor
  - Observer Service 가 있어야 하네
    - CodeGeneration 때 Channel 이 ActorNotificationChannel 가 아니면 디지게 하는 처리가 필요하다!

## StopMessage 의 문제
  - RoomDirectory 와 같이 Child Actor 를 관리하는 Actor 들은
    Child 가 모두 죽을 때까지 Stop 될 수 없다. 그래서 보통 다음과 같이 구현한다.
    ```
    async OnStop()
    {
        foreach (var child in children)
            child.Tell(StopMessage.Instance)
        await WaitForAllChildrenStop();
    }
    ```
    문제는 보통 `WaitForAllChildrenStop` 구현이 아래와 같이 Child actor 가 보내는
    OnUnregister 등의 메세지에 의존한다는 것이다. Stop 핸들러가 atomic 이라 OnUnregister 를
    처리할 수가 없다. (일종의 데드락 상태가 된다)
    ```
    async OnUnregister()
    {
        ...
    }
    ```
  - 그래서 이 문제를 해결하려면 Stop() 쪽은 async 로 해서는 안된다.
    ClientGateway 는 다음과 같이 해결했다.
    ```
    void OnStop()
    {
        stopped = true;
        foreach (var child in children)
            child.Tell(StopMessage.Instance)
        if (#children == 0) Stop();
    }
    void OnUnregister()
    {
        if (stopped && #children == 0) Stop();
    }
    ```

## ETC
  - 주석좀
  - 코딩 컨벤션 붙여서 코드 정리! 으갸으갸
  - 여러 프레임웍 지원하는 프로젝트 구조도 잡아보자.
    - 당장 .NET 3.5 와 2.0 지원을 넣어서 잘 정리해 보는게?
	- 솔루션이 커졌는데 예제는 분리할까?
      local nuget 이 가능하면 굳이 core 랑 example 이 같이 있을 필요는 없다

## SlimUnity
  - 일단 Communicator!
    - 이거 대충 돌아만 가도록 만들어 뒀는데 requirement 정리해서 잘 구현하자.
	- 먼저 접속 유지 기능
	- RequestId 로 연결 재개 하는 것도?
	- 세션 쪽도 고민해보고
  - 서버쪽은 따로 분리할 수 있을 듯
    - 당장 급하지는 않는데 결국 해야 할 방향
	- ClientSession, ClientGateway 와 UserActor, UserAuthenticator 를 잘 붙여보자.

# 아이디어
 
## Akka.net 에 .NET 4, 3.5 지원 얘기가 있다
  https://github.com/akkadotnet/akka.net/issues/1313

## Unity 용 Task 가 있네?
  http://spicypixel.com/developer/concurrency-kit/

# 참고

## http://spicypixel.com/developer/concurrency-kit/learn/

# 장기 노트

## Serializer
  - protobuf-net 이 가지는 한계
    - polymorphism 을 지원하지 않는 거
	- surrogate 가 딱 그 타입만 지원하는 거 (상속 관계를 지원하지 않음)
  - 지금은 어떻게든 꾸역꾸역 버티고 있는데 이거 금방 바닥난다.
  - 예를 들어 rpc 인자 ICounter 타입을 메시지 클래스에서는 CounterRef 로 바꿔 보내느데
    이런건 함수 매개변수로만 가능하고 클래스로 한번 wrapping 하면 그냥 오류난다.
	polymorphism 혹은 상속 지원 surrogate 가 있어야 해결 가능

## cluster sharding 
  - 현재 akka.net 1.0.4 에는 cluster sharding 이 없다.
  - 없어서 그냥 노드중 하나가 directory 서비스를 지원하도록 되어 있다. (SPOF)
  - 나중에 sharding 들어가면 거기에 잘 얹어 보자.
