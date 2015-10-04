using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Akka.Interfaced
{
    public delegate ResponseMessage RequestHandler<in T>(T self, RequestMessage request, Action<ResponseMessage> onCompleted);
    public delegate Task<ResponseMessage> RequestAsyncHandler<in T>(T self, RequestMessage request, Action<ResponseMessage> onCompleted);

    public static class RequestHandlerBuilder
    {
        public static RequestHandler<T> BuildHandler<T>(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method,
            IList<IPreHandleFilter> preHandleFilters, IList<IPostHandleFilter> postHandleFilters)
            where T : class
        {
            var handler = RequestHandlerFuncBuilder.Build<T>(
                invokePayloadType, returnPayloadType, method);

            return delegate (T self, RequestMessage request, Action<ResponseMessage> onCompleted)
            {
                ResponseMessage response = null;

                // Call PreHandleFilters

                if (preHandleFilters.Count > 0)
                {
                    var context = new PreHandleFilterContext
                    {
                        Actor = self,
                        Request = request
                    };
                    foreach (var filter in preHandleFilters)
                    {
                        try
                        {
                            filter.OnPreHandle(context);
                            if (context.Response != null)
                            {
                                response = context.Response;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                        }
                    }
                }

                // Call Handler

                if (response == null)
                {
                    try
                    {
                        var returnPayload = handler(self, request.InvokePayload);
                        response = new ResponseMessage
                        {
                            RequestId = request.RequestId,
                            ReturnPayload = returnPayload
                        };
                    }
                    catch (Exception e)
                    {
                        response = new ResponseMessage
                        {
                            RequestId = request.RequestId,
                            Exception = e
                        };
                    }
                }

                // Call PostHandleFilters

                if (postHandleFilters.Count > 0)
                {
                    var context = new PostHandleFilterContext
                    {
                        Actor = self,
                        Request = request,
                        Response = response
                    };
                    foreach (var filter in postHandleFilters)
                    {
                        try
                        {
                            filter.OnPostHandle(context);
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                        }
                    }
                }

                if (onCompleted != null)
                    onCompleted(response);

                return response;
            };
        }

        public static RequestAsyncHandler<T> BuildAsyncHandler<T>(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method,
            IList<IFilter> preHandleFilters, IList<IFilter> postHandleFilters) 
            where T : class
        {
            var isAsyncMethod = method.ReturnType.Name.StartsWith("Task");
            var handler = isAsyncMethod
                ? RequestHandlerAsyncBuilder.Build<T>(invokePayloadType, returnPayloadType, method)
                : RequestHandlerSyncToAsyncBuilder.Build<T>(invokePayloadType, returnPayloadType, method);

            // TODO: Optimize this function when without async filter
            return async delegate(T self, RequestMessage request, Action<ResponseMessage> onCompleted)
            {
                ResponseMessage response = null;

                // Call PreHandleFilters

                if (preHandleFilters.Count > 0)
                {
                    var context = new PreHandleFilterContext
                    {
                        Actor = self,
                        Request = request
                    };
                    foreach (var filter in preHandleFilters)
                    {
                        try
                        {
                            var preFilter = filter as IPreHandleFilter;
                            if (preFilter != null)
                            {
                                preFilter.OnPreHandle(context);
                            }
                            else
                            {
                                var preAsyncFilter = filter as IPreHandleAsyncFilter;
                                if (preAsyncFilter != null)
                                {
                                    await preAsyncFilter.OnPreHandle(context);
                                }
                            }

                            if (context.Response != null)
                            {
                                response = context.Response;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                        }
                    }
                }

                // Call Handler

                if (response == null)
                {
                    try
                    {
                        var returnPayload = await handler(self, request.InvokePayload);
                        response = new ResponseMessage
                        {
                            RequestId = request.RequestId,
                            ReturnPayload = returnPayload
                        };
                    }
                    catch (Exception e)
                    {
                        response = new ResponseMessage
                        {
                            RequestId = request.RequestId,
                            Exception = e
                        };
                    }
                }

                // Call PostHandleFilters

                if (postHandleFilters.Count > 0)
                {
                    var context = new PostHandleFilterContext
                    {
                        Actor = self,
                        Request = request,
                        Response = response
                    };
                    foreach (var filter in postHandleFilters)
                    {
                        try
                        {
                            var postFilter = filter as IPostHandleFilter;
                            if (postFilter != null)
                            {
                                postFilter.OnPostHandle(context);
                            }
                            else
                            {
                                var postAsyncFilter = filter as IPostHandleAsyncFilter;
                                if (postAsyncFilter != null)
                                {
                                    await postAsyncFilter.OnPostHandle(context);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                        }
                    }
                }

                if (onCompleted != null)
                    onCompleted(response);

                return response;
            };
        }
    }
}
