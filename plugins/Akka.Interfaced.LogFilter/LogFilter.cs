using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Akka.Interfaced.LogFilter
{
    internal class LogFilter : IPreRequestFilter, IPostRequestFilter, IPreMessageFilter, IPreNotificationFilter
    {
        private static readonly JsonSerializerSettings _settings;
        private readonly LogFilterTarget _target;
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

        public LogFilter(LogFilterTarget target, int filterOrder, ILogProxy logProxy, MethodInfo method)
        {
            _target = target;
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
            if (_target.HasFlag(LogFilterTarget.Request) == false || _logProxy.IsEnabled(context.Actor) == false)
                return;

            var invokeJson = GetValueString(context.Request.InvokePayload);
            _logProxy.Log(
                context.Actor,
                $"<- (#{context.Request.RequestId}) {_methodShortName} {invokeJson}");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            if (_target.HasFlag(LogFilterTarget.Request) == false || _logProxy.IsEnabled(context.Actor) == false)
                return;

            if (context.Exception != null)
            {
                _logProxy.Log(
                    context.Actor,
                    $"-> (#{context.Request.RequestId}) {_methodShortName} Fault: {context.Exception}");
            }
            else if (context.Response != null)
            {
                var r = context.Response;
                if (r.Exception != null)
                {
                    _logProxy.Log(
                        context.Actor,
                        $"-> (#{context.Request.RequestId}) {_methodShortName} Exception: {r.Exception}");
                }
                else if (r.ReturnPayload != null)
                {
                    var value = GetValueString(r.ReturnPayload.Value);
                    _logProxy.Log(
                        context.Actor,
                        $"-> (#{context.Request.RequestId}) {_methodShortName} {value}");
                }
                else
                {
                    _logProxy.Log(
                        context.Actor,
                        $"-> (#{context.Request.RequestId}) {_methodShortName} <void>");
                }
            }
            else
            {
                _logProxy.Log(
                    context.Actor,
                    $"-> (#{context.Request.RequestId}) {_methodShortName} <null>");
            }
        }

        void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
        {
            if (_target.HasFlag(LogFilterTarget.Message) == false || _logProxy.IsEnabled(context.Actor) == false)
                return;

            var invokeJson = GetValueString(context.Message);
            _logProxy.Log(
                context.Actor,
                $"<- {_methodShortName} {context.Message.GetType().Name}({invokeJson})");
        }

        void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
        {
            if (_target.HasFlag(LogFilterTarget.Notification) == false || _logProxy.IsEnabled(context.Actor) == false)
                return;

            var invokeJson = GetValueString(context.Notification.InvokePayload);
            _logProxy.Log(
                context.Actor,
                $"<- {_methodShortName} {invokeJson}");
        }
    }
}
