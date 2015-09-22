using System;
using UnityEngine;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using SlimUnityChat.Interface;
using System.Collections.Generic;

public class TestScene : MonoBehaviour
{
    private UserRef _user;
    private string _userId;
    private int _lastObserverId;
    private Dictionary<string, OccupantRef> _occupantMap = new Dictionary<string, OccupantRef>();
    private Dictionary<string, int> _observerMap = new Dictionary<string, int>();

    void Start()
    {
        ApplicationComponent.TryInit();

        G.Comm = new Communicator(G.Logger, this);
        G.Comm.ServerEndPoint = new IPEndPoint(IPAddress.Loopback, 9000);
        G.Comm.Start();

        StartCoroutine(ProcessTest());
    }

    private IEnumerator ProcessTest()
    {
        yield return new WaitForSeconds(1);

        Debug.Log("Start ProcessTest");

        yield return StartCoroutine(ProcessLoginUser());

        yield return StartCoroutine(ProcessEnterRoom("room1"));
        yield return StartCoroutine(ProcessEnterRoom("room2"));

        yield return StartCoroutine(ProcessSay("room1", "Hello Room1!"));
        yield return StartCoroutine(ProcessSay("room2", "Hello Room2!"));

        yield return StartCoroutine(ProcessExitFromRoom("room1"));
        yield return StartCoroutine(ProcessExitFromRoom("room2"));
    }

    private IEnumerator ProcessLoginUser()
    {
        Debug.LogFormat("ProcessLoginUser");

        var userLogin = new UserLoginRef(new SlimActorRef { Id = 1 }, new SlimRequestWaiter { Communicator = G.Comm }, null);

        // Login

        var t1 = userLogin.Login("tester", "1234", G.Comm.IssueObserverId());
        yield return t1.WaitHandle;
        ShowResult(t1, "Login");
        if (t1.Exception != null)
            yield break;
        _user = new UserRef(new SlimActorRef { Id = t1.Result }, new SlimRequestWaiter { Communicator = G.Comm }, null);

        // Get UserId (just for test)

        var t2 = _user.GetId();
        yield return t2.WaitHandle;
        ShowResult(t2, "GetId");
        _userId = t2.Result;
    }

    private IEnumerator ProcessEnterRoom(string roomName)
    {
        var observerId = ++_lastObserverId;
        G.Comm.AddObserver(observerId, new TestRoomObserver(roomName));

        var t1 = _user.EnterRoom(roomName, observerId);
        yield return t1.WaitHandle;
        ShowResult(t1, string.Format("EnterRoom({0})", roomName));

        if (t1.Status == TaskStatus.RanToCompletion)
        {
            var occupant = new OccupantRef(new SlimActorRef { Id = t1.Result.Item1 }, new SlimRequestWaiter { Communicator = G.Comm }, null);
            _occupantMap[roomName] = occupant;
            _observerMap[roomName] = observerId;
            Debug.LogFormat("Room({0}) has {1} users.", roomName, t1.Result.Item2.Users.Count);
        }
        else
        {
            G.Comm.RemoveObserver(observerId);
        }
    }

    private IEnumerator ProcessExitFromRoom(string roomName)
    {
        var t1 = _user.ExitFromRoom(roomName);
        yield return t1.WaitHandle;
        ShowResult(t1, string.Format("ExitFromRoom({0})", roomName));

        if (t1.Status == TaskStatus.RanToCompletion)
        {
            G.Comm.RemoveObserver(_observerMap[roomName]);
            _occupantMap.Remove(roomName);
            _observerMap.Remove(roomName);
        }
    }

    private IEnumerator ProcessSay(string roomName, string msg)
    {
        OccupantRef occupant;
        if (_occupantMap.TryGetValue(roomName, out occupant) == false)
            yield break;

        var t1 = occupant.Say(_userId, msg);
        yield return t1.WaitHandle;
        ShowResult(t1, string.Format("ProcessSay({0}, {1})", roomName, msg));
    }

    void ShowResult(Task task, string name)
    {
        if (task.Status == TaskStatus.RanToCompletion)
            Debug.Log(string.Format("{0}: Done", name));
        else if (task.Status == TaskStatus.Faulted)
            Debug.Log(string.Format("{0}: Exception = {1}", name, task.Exception));
        else if (task.Status == TaskStatus.Canceled)
            Debug.Log(string.Format("{0}: Canceled", name));
        else
            Debug.Log(string.Format("{0}: Illegal Status = {1}", name, task.Status));
    }

    void ShowResult<TResult>(Task<TResult> task, string name)
    {
        if (task.Status == TaskStatus.RanToCompletion)
            Debug.Log(string.Format("{0}: Result = {1}", name, task.Result));
        else
            ShowResult((Task)task, name);
    }
}
