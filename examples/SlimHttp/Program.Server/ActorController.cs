using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Akka;
using Akka.Actor;
using Akka.Interfaced;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TypeAlias;

namespace SlimHttp.Program.Server
{
    public class ActorController : ApiController
    {
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

        [HttpPost, Route("actor/{id}")]
        public async Task<HttpResponseMessage> Actor(string id, ActorRequest request)
        {
            // Get Message

            IAsyncInvokable message;
            try
            {
                var jsonText = await Request.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonText);

                var type = TypeUtility.GetType(request.MessageType);
                if (type == null)
                {
                    return Request.CreateResponse(
                        HttpStatusCode.BadRequest,
                        "Cannot find message type: " + request.MessageType);
                }

                message = (IAsyncInvokable)Activator.CreateInstance(type);
                JsonConvert.PopulateObject(request.MessageData.ToString(), message);

                Console.WriteLine("* Actor({0}) <- {1} {2}", id, type.Name, request.MessageData.ToString(Formatting.None));
            }
            catch (Exception e)
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    e.ToString());
            }
           
            // Try to send message to actor

            IActorRef actor;
            try
            {
                actor = await Program.System.ActorSelection("/user/" + id).ResolveOne(TimeSpan.Zero);
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "Actor not found");
            }

            if (request.RequestId != 0)
            {
                var reply = await actor.Ask<ResponseMessage>(new RequestMessage
                {
                    RequestId = request.RequestId,
                    InvokePayload = message
                });
                if (reply != null)
                {
                    var reply2 = new ActorReply { RequestId = reply.RequestId };
                    if (reply.Exception != null)
                    {
                        reply2.Exception = reply.Exception;
                        Console.WriteLine("* Actor({0}) -> {1}", id, reply.Exception);
                    }
                    else if (reply.ReturnPayload != null)
                    {
                        reply2.ResultType = reply.ReturnPayload.GetType().FullName;
                        var value = reply.ReturnPayload.Value;
                        if (value != null)
                        {
                            reply2.ResultData = JToken.FromObject(value);
                            Console.WriteLine("* Actor({0}) -> {1} {2}", id, reply.ReturnPayload.GetType().Name, reply2.ResultData.ToString(Formatting.None));
                        }
                        else
                        {
                            Console.WriteLine("* Actor({0}) -> null", id);
                        }
                    }
                    else
                    {
                        Console.WriteLine("* Actor({0}) -> void", id);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, reply2);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "?");
                }
            }
            else
            {
                actor.Tell(new RequestMessage {InvokePayload = message});
                return Request.CreateResponse(HttpStatusCode.OK);
            }
        }
    }
}
