using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocketBase;
using Akka.Interfaced.SlimSocketClient;

namespace PingpongProgram.ClientLib
{
    class SlimRequestWaiter : IRequestWaiter
    {
        private readonly TcpConnection _connection;
        private int _lastRequestId;
        private ConcurrentDictionary<int, Action<ResponseMessage>> _requestResponseMap = 
            new ConcurrentDictionary<int, Action<ResponseMessage>>();

        public SlimRequestWaiter(TcpConnection connection)
        {
            _connection = connection;
            _connection.Received += OnRecvPacket;
        }

        void IRequestWaiter.SendRequest(IActorRef target, RequestMessage requestMessage)
        {
            SendRequest(target, requestMessage);
        }

        Task IRequestWaiter.SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            return SendRequestAndWait(target, requestMessage, timeout);
        }

        Task<T> IRequestWaiter.SendRequestAndReceive<T>(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            return SendRequestAndReceive<T>(target, requestMessage, timeout);
        }

        internal void SendRequest(IActorRef target, RequestMessage requestMessage)
        {
            SendRequestPacket(new Packet
            {
                Type = PacketType.Request,
                ActorId = ((SlimActorRef)target).Id,
                Message = requestMessage.InvokePayload,
            }, null);
        }

        internal Task SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            var tcs = new TaskCompletionSource<object>();
            SendRequestPacket(new Packet
            {
                Type = PacketType.Request,
                ActorId = ((SlimActorRef)target).Id,
                Message = requestMessage.InvokePayload,
            }, r =>
            {
                if (r.Exception != null)
                    tcs.SetException(r.Exception);
                else
                    tcs.SetResult(null);
            });
            return tcs.Task;
        }

        internal Task<T> SendRequestAndReceive<T>(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            var tcs = new TaskCompletionSource<T>();
            SendRequestPacket(new Packet
            {
                Type = PacketType.Request,
                ActorId = ((SlimActorRef)target).Id,
                Message = requestMessage.InvokePayload,
            }, r =>
            {
                if (r.Exception != null)
                    tcs.SetException(r.Exception);
                else
                    tcs.SetResult((T)(r.ReturnPayload.Value));
            });
            return tcs.Task;
        }

        private void SendRequestPacket(Packet packet, Action<ResponseMessage> completionHandler)
        {
            packet.RequestId = ++_lastRequestId;

            if (completionHandler != null)
            {
                var added = _requestResponseMap.TryAdd(packet.RequestId, completionHandler);
                if (added == false)
                    throw new InvalidOperationException("Failed to add");
            }

            _connection.SendPacket(packet);
        }

        private void OnRecvPacket(object sender, object packet)
        {
            var p = (Packet)packet;
            switch (p.Type)
            {
                case PacketType.Response:
                    Action<ResponseMessage> handler;
                    if (_requestResponseMap.TryRemove(p.RequestId, out handler))
                    {
                        handler(new ResponseMessage
                        {
                            RequestId = p.RequestId,
                            ReturnPayload = (IValueGetable)p.Message,
                            Exception = p.Exception
                        });
                    }
                    break;
            }
        }
    }
}
