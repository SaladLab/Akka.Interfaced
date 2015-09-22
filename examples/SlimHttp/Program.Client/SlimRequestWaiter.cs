using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TypeAlias;

namespace SlimHttp.Program.Client
{
    class SlimRequestWaiter : ISlimRequestWaiter
    {
        public Uri Root { get; set; }

        public class ActorRequest
        {
            public int RequestId { get; set; }
            public string MessageType { get; set; }
            public JObject MessageData { get; set; }
        }

        public class ActorReply
        {
            public int RequestId { get; set; }
            public string ResultType { get; set; }
            public JToken ResultData { get; set; }
            public Exception Exception { get; set; }
        }

        private int _lastRequestId;

#if NET20 || NET35
        Task ISlimRequestWaiter.SendRequestAndWait(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
        {
            var httpWebRequest = SendRequest(target, requestMessage, timeout);
            var task = new SlimTask();
            task.WebRequest = httpWebRequest;
            task.WaitEvent = new ManualResetEvent(false);
            httpWebRequest.BeginGetResponse(new AsyncCallback(FinishRequest), task);
            return task;
        }

        Task<T> ISlimRequestWaiter.SendRequestAndReceive<T>(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
        {
            var httpWebRequest = SendRequest(target, requestMessage, timeout);
            var task = new SlimTask<T>();
            task.WebRequest = httpWebRequest;
            task.WaitEvent = new ManualResetEvent(false);
            httpWebRequest.BeginGetResponse(new AsyncCallback(FinishRequest<T>), task);
            return task;
        }

        private void FinishRequest(IAsyncResult result)
        {
            // Get result from stream

            var task = (result.AsyncState as SlimTask);
            var response = task.WebRequest.EndGetResponse(result);
            var reply = GetReplyFromStream(response.GetResponseStream());

            // Make a task completed

            if (reply.Exception == null)
            {
                task.Status = TaskStatus.RanToCompletion;
                task.WaitEvent.Set();
            }
            else
            {
                task.Exception = reply.Exception;
            }
        }

        private void FinishRequest<T>(IAsyncResult result)
        {
            // Get result from stream

            var task = (result.AsyncState as SlimTask<T>);
            var response = task.WebRequest.EndGetResponse(result);
            var reply = GetReplyFromStream(response.GetResponseStream());

            // Make a task completed

            if (reply.Exception == null)
            {
                task.Result = GetResultFromReply<T>(reply.ResultType, reply.ResultData);
            }
            else
            {
                task.Exception = reply.Exception;
            }
        }
#else
        async Task ISlimRequestWaiter.SendRequestAndWait(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
        {
            // Get result from stream

            var httpWebRequest = SendRequest(target, requestMessage, timeout);
            var response = await httpWebRequest.GetResponseAsync();
            var reply = GetReplyFromStream(response.GetResponseStream());

            if (reply.Exception != null)
                throw reply.Exception;
        }

        async Task<T> ISlimRequestWaiter.SendRequestAndReceive<T>(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
        {
            // Get result from stream

            var httpWebRequest = SendRequest(target, requestMessage, timeout);
            var response = await httpWebRequest.GetResponseAsync();
            var reply = GetReplyFromStream(response.GetResponseStream());

            if (reply.Exception != null)
                throw reply.Exception;

            return GetResultFromReply<T>(reply.ResultType, reply.ResultData);
        }
#endif

        private HttpWebRequest SendRequest(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
        {
            var actorId = ((SlimActorRef)target).Id;
            var uri = new Uri(Root, "/actor/" + actorId);

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            var actorRequest = new ActorRequest
            {
                RequestId = ++_lastRequestId,
                MessageType = requestMessage.Message.GetType().FullName,
                MessageData = JObject.Parse(JsonConvert.SerializeObject(requestMessage.Message))
            };

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(JsonConvert.SerializeObject(actorRequest));
            }

            return httpWebRequest;
        }

        private ActorReply GetReplyFromStream(Stream stream)
        {
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<ActorReply>(jsonTextReader);
            }
        }

        private T GetResultFromReply<T>(string resultType, JToken resultData)
        {
            var type = TypeUtility.GetType(resultType);
            var envelopedValueJson = (new JObject(new JProperty("v", resultData))).ToString();
            var result = (T)(((IValueGetable)JsonConvert.DeserializeObject(envelopedValueJson, type)).Value);
            return result;
        }
    }
}
