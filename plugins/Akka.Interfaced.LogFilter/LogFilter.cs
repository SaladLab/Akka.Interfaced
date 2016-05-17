using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Akka.Interfaced.LogFilter
{
    internal class LogFilter : IPreRequestFilter, IPostRequestFilter
    {
        private static readonly JsonSerializerSettings _settings;
        private readonly int _filterOrder;
        private readonly ILogProxy _logProxy;
        private readonly string _methodShortName;

        static LogFilter()
        {
            _settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new SurrogateSimpleConverter() },
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }

        public LogFilter(int filterOrder, ILogProxy logProxy, MethodInfo method)
        {
            _filterOrder = filterOrder;
            _logProxy = logProxy;
            _methodShortName = method.Name.Split('.').Last();
        }

        private static string GetValueString(object obj)
        {
            return JsonConvert.SerializeObject(obj, _settings);
        }

        int IFilter.Order => _filterOrder;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            if (_logProxy.IsEnabled(context.Actor) == false)
                return;

            var invokeJson = GetValueString(context.Request.InvokePayload);
            _logProxy.Log(context.Actor,
                        $"#{context.Request.RequestId} -> {_methodShortName} {invokeJson}");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            if (_logProxy.IsEnabled(context.Actor) == false)
                return;

            if (context.Response.Exception != null)
            {
                _logProxy.Log(context.Actor,
                            $"#{context.Request.RequestId} <- {_methodShortName} Exception: {context.Response.Exception}");
            }
            else if (context.Response.ReturnPayload != null)
            {
                var value = GetValueString(context.Response.ReturnPayload.Value);
                _logProxy.Log(context.Actor,
                            $"#{context.Request.RequestId} <- {_methodShortName} {value}");
            }
            else
            {
                _logProxy.Log(context.Actor,
                            $"#{context.Request.RequestId} <- {_methodShortName} <void>");
            }
        }
    }
}
