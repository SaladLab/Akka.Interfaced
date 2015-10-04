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
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class LogAttribute : Attribute, IFilterFactory
    {
        IFilter IFilterFactory.CreateInstance(Type actorType, MethodInfo method)
        {
            return new LogFilter(actorType, method);
        }
    }

    public class LogFilter : IPreHandleFilter, IPostHandleFilter
    {
        private static readonly JsonSerializerSettings _settings;
        private FieldInfo _loggerFieldInfo;
        private string _methodShortName;

        static LogFilter()
        {
            _settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new SurrogateSimpleConverter() },
            };
        }

        public LogFilter(Type actorType, MethodInfo method)
        {
            _loggerFieldInfo = actorType.GetField("_logger", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _methodShortName = method.Name.Split('.').Last();
        }

        int IFilter.Order
        {
            get
            {
                return 0;
            }
        }

        private ILog GetLogger(object actor)
        {
            return (ILog)_loggerFieldInfo.GetValue(actor);
        }

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            var logger = GetLogger(context.Actor);
            var invokeJson = JsonConvert.SerializeObject(context.Request.InvokePayload, _settings);
            logger.TraceFormat("#{0} -> {1} {2}",
                               context.Request.RequestId, _methodShortName, invokeJson);
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            var logger = GetLogger(context.Actor);
            if (context.Response.Exception != null)
            {
                logger.TraceFormat("#{0} <- {1} Exception: {2}",
                                   context.Request.RequestId, _methodShortName, context.Response.Exception);
            }
            else if (context.Response.ReturnPayload == null)
            {
                var returnJson = JsonConvert.SerializeObject(context.Response.ReturnPayload, _settings);
                logger.TraceFormat("#{0} <- {1} {2}",
                                   context.Request.RequestId, _methodShortName, returnJson);
            }
            else
            {
                logger.TraceFormat("#{0} <- {1} <void>",
                                   context.Request.RequestId, _methodShortName);
            }
        }
    }

    // This is quite simple class for dealing with serializing ISurrogated instances.
    // Without this converter, json default serailizer easily gets lost in inspecting IActorRef object.
    // Because sole purpose is just writing log, it uses ToString instead of ISurrogated context.
    internal class SurrogateSimpleConverter : JsonConverter
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
}
