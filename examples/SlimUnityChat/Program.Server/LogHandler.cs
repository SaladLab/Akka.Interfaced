using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Akka.Interfaced;
using Newtonsoft.Json;
using Common.Logging;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;

namespace SlimUnityChat.Program.Server
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LogAttribute : Attribute
    {
    }

    public static class LogHandlerBuilder
    {
        private static readonly JsonSerializerSettings _settings;

        static LogHandlerBuilder()
        {
            _settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new SurrogateSimpleConverter() },
            };
        }

        // This is quite simple class for dealing with serializing ISurrogated instances.
        // Without this converter, json default serailizer easily gets lost in inspecting IActorRef object.
        // Because sole purpose is just writing log, it uses ToString instead of ISurrogated context.
        private class SurrogateSimpleConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                if (typeof(Akka.Util.ISurrogated).IsAssignableFrom(objectType))
                    return true;

                return false;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }
        }

        public static RequestMessageHandler<T> BuildHandler<T>(
            Func<T, ILog> logHandler,
            RequestMessageHandler<T> handler,
            MethodInfo method,
            bool inclusive = false)
            where T : InterfacedActor<T>
        {

            var hasLogAttribute = method.CustomAttributes.Any(x => x.AttributeType == typeof(LogAttribute));
            if (hasLogAttribute || inclusive)
            {
                var methodShortName = method.Name.Split('.').Last();
                return async delegate (T self, RequestMessage requestMessage)
                {
                    ILog log = null;
                    try
                    {
                        log = logHandler(self);
                        var requestName = requestMessage.InvokePayload.GetType().Name;
                        var requestJson = JsonConvert.SerializeObject(requestMessage.InvokePayload, _settings);
                        log.TraceFormat(
                            "#{0} -> {1} {2}",
                            requestMessage.RequestId, methodShortName, requestJson);
                    }
                    catch (Exception e)
                    {
                        if (log != null)
                            log.FatalFormat("LogHandler got an exception in intro of {0}", e, methodShortName);
                        return null;
                    }

                    var watch = new Stopwatch();
                    var ret = await handler(self, requestMessage);
                    var elapsed = watch.ElapsedMilliseconds;

                    if (log != null)
                    {
                        try
                        {
                            if (ret != null)
                            {
                                var replyJson = JsonConvert.SerializeObject(ret.Value, _settings);
                                log.TraceFormat(
                                    "#{0} <- {1} {2} ({3}ms)",
                                    requestMessage.RequestId, methodShortName, replyJson, elapsed);
                            }
                            else
                            {
                                log.TraceFormat(
                                    "#{0} <- {1} ({2}ms)",
                                    requestMessage.RequestId, methodShortName, elapsed);
                            }
                        }
                        catch (Exception e)
                        {
                            if (log != null)
                                log.FatalFormat("LogHandler got an exception in outro of {0}", e, methodShortName);
                        }
                    }
                    return ret;
                };
            }
            else
            {
                return handler;
            }
        }
    }
}
