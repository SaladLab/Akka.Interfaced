using System;
using UnityEngine;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using SlimUnity.Interface;

public class MainScene : MonoBehaviour 
{
	void Start()
	{
        G.Comm = new Communicator(G.Logger, this);
        G.Comm.ServerEndPoint = new IPEndPoint(IPAddress.Loopback, 9000);
        G.Comm.Start();

        StartCoroutine(ProcessTest());
    }

	void Update()
	{
        G.Comm.Update();
	    G.UnityLogger.Flush();
	}

    IEnumerator ProcessTest()
    {
        yield return new WaitForSeconds(1);
        
        Debug.Log("Start ProcessTest");

        yield return StartCoroutine(ProcessTestCounter());
        yield return StartCoroutine(ProcessTestCalculator());
        yield return StartCoroutine(ProcessTestPedantic());
    }

    IEnumerator ProcessTestCounter()
    {
        Debug.Log("*** Counter ***");

        var counter = new CounterRef(new SlimActorRef { Id = 1 }, new SlimRequestWaiter { Communicator = G.Comm }, null);

        yield return counter.IncCounter(1).WaitHandle;
        yield return counter.IncCounter(2).WaitHandle;
        yield return counter.IncCounter(3).WaitHandle;

        var t1 = counter.IncCounter(-1);
        yield return t1.WaitHandle;
        ShowResult(t1, "IncCount(-1)");

        var t2 = counter.GetCounter();
        yield return t2.WaitHandle;
        ShowResult(t2, "GetCounter");
    }

    IEnumerator ProcessTestCalculator()
    {
        Debug.Log("*** Calculator ***");

        var calculator = new CalculatorRef(new SlimActorRef { Id = 2 }, new SlimRequestWaiter { Communicator = G.Comm }, null);

        var t1 = calculator.Sum(1, 2);
        yield return t1.WaitHandle;
        ShowResult(t1, "Sum(1, 2)");

        var t2 = calculator.Sum(Tuple.Create(2, 3));
        yield return t2.WaitHandle;
        ShowResult(t2, "Sum((2, 3))");

        var t3 = calculator.Concat("Hello", "World");
        yield return t3.WaitHandle;
        ShowResult(t3, "Concat(Hello, World)");

        var t4 = calculator.Concat("Hello", null);
        yield return t4.WaitHandle;
        ShowResult(t4, "Concat(Hello, null)");
    }

    IEnumerator ProcessTestPedantic()
    {
        Debug.Log("*** Pedantic ***");

        var pedantic = new PedanticRef(new SlimActorRef { Id = 3 }, new SlimRequestWaiter { Communicator = G.Comm }, null);

        var t1 = pedantic.TestCall();
        yield return t1.WaitHandle;
        ShowResult(t1, "TestCall");

        var t2 = pedantic.TestOptional(10);
        yield return t2.WaitHandle;
        ShowResult(t2, "TestOptional(10)");

        var t3 = pedantic.TestTuple(Tuple.Create(1, "one"));
        yield return t3.WaitHandle;
        ShowResult(t3, "TestTuple");

        var t4 = pedantic.TestParams(1, 2, 3);
        yield return t4.WaitHandle;
        ShowResult(t4, "TestParams");

        var t5 = pedantic.TestPassClass(new TestParam { Name = "Mouse", Price = 10 });
        yield return t5.WaitHandle;
        ShowResult(t5, "TestPassClass");

        var t6 = pedantic.TestReturnClass(10, 5);
        yield return t6.WaitHandle;
        ShowResult(t6, "TestReturnClass");
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
