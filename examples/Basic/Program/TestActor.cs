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
    public sealed class LogAttribute : Attribute, IFilterFactory, IPreHandleFilter, IPostHandleFilter
    {
        int IFilter.Order
        {
            get
            {
                return 0;
            }
        }

        IFilter IFilterFactory.CreateInstance(Type actorType, MethodInfo method)
        {
            return this;
        }

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            var requestName = context.Request.InvokePayload.GetType().Name;
            var requestJson = JsonConvert.SerializeObject(context.Request.InvokePayload, Formatting.None);
            Console.WriteLine("* Invoke: {0} #{1} <{2}>",
                              requestName, context.Request.RequestId, requestJson);
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            var requestName = context.Request.InvokePayload.GetType().Name;
            if (context.Response.Exception != null)
            {
                Console.WriteLine("* Return: {0} #{1} Exception: {2}", 
                                  requestName, context.Request.RequestId, context.Response.Exception);
            }
            else if (context.Response.ReturnPayload != null)
            {
                var returnJson = JsonConvert.SerializeObject(context.Response.ReturnPayload, Formatting.None);
                Console.WriteLine("* Return: {0} #{1} <{2}>",
                                  requestName, context.Request.RequestId, returnJson);
            }
            else
            {
                Console.WriteLine("* Return: {0} #{1} <void>",
                                  requestName, context.Request.RequestId);
            }
        }
    }

    public class TestActor : InterfacedActor<TestActor>, ICalculator, ICounter, IWorker
    {
        private int _counter;

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
