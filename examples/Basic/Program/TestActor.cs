using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;
using Newtonsoft.Json;
using System.Threading;
using Basic.Interface;

namespace Basic.Program
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LogAttribute : Attribute
    {
    }

    public class TestActor : InterfacedActor<TestActor>, ICalculator, ICounter, IWorker
    {
        private int _counter;

        /*
        protected static RequestHandler<TestActor> OnBuildHandler(
            RequestHandler<TestActor> handler, MethodInfo method)
        {
            var hasLogAttribute = method.CustomAttributes.Any(x => x.AttributeType == typeof(LogAttribute));
            if (hasLogAttribute)
            {
                return async delegate(TestActor self, RequestMessage requestMessage)
                {
                    var requestName = requestMessage.InvokePayload.GetType().Name;
                    var requestJson = JsonConvert.SerializeObject(requestMessage.InvokePayload, Formatting.None);
                    Console.WriteLine("* Request: {0} #{1} <{2}>", requestName, requestMessage.RequestId, requestJson);
                    
                    var watch = new Stopwatch();
                    var ret = await handler(self, requestMessage);
                    var elapsed = watch.ElapsedMilliseconds;

                    var replyJson = JsonConvert.SerializeObject(ret, Formatting.None);
                    Console.WriteLine("* Reply  : {0} #{1} <{2}> elapsed: {3}ms", requestName, requestMessage.RequestId, replyJson, elapsed);
                    return ret;
                };
            }
            else
            {
                return handler;
            }
        }
        */

        [Log]
        Task<string> ICalculator.Concat(string a, string b)
        {
            return Task.FromResult(a + b);
        }

        [Log]
        Task<int> ICalculator.Sum(int a, int b)
        {
            return Task.FromResult(a + b);
        }

        [Log]
        Task ICounter.IncCounter(int delta)
        {
            _counter += delta;
            return Task.FromResult(0);
        }

        [Log]
        Task<int> ICounter.GetCounter()
        {
            return Task.FromResult(_counter);
        }

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
}
