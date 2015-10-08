using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Akka.Interfaced.SlimSocketBase;
using Common.Logging;

namespace Akka.Interfaced.SlimSocketClient
{
    public class TcpConnection
    {
        public enum TcpState
        {
            Closed,
            Closing,
            Connected,
            Connecting,
        }

        private TcpState _state;
        private TcpClient _client;
        private EndPoint _localEndPoint;
        private EndPoint _remoteEndPoint;
        private ILog _logger;

        private byte[] _recvBuf = new byte[131072];
        private int _recvBufLen;
        private DateTime _lastReceiveTime;

        private byte[] _sendBuf = new byte[131072];
        private List<object> _sendPacketQueue = new List<object>();
        private volatile bool _sendProcessing;

        private readonly IPacketSerializer _packetSerializer;

        public TcpState State
        {
            get { return _state; }
        }

        public IPacketSerializer PacketSerializer
        {
            get { return _packetSerializer; }
        }

        public DateTime LastReceiveTime
        {
            get { return _lastReceiveTime; }
        }

        public event Action<object> Connected;
        public event Action<object, int> Closed;
        public event Action<object, object> Received;

        public TcpConnection(IPacketSerializer serializer, ILog logger)
        {
            _state = TcpState.Closed;
            _logger = logger;
            _packetSerializer = serializer;
        }

        public void Connect(IPEndPoint remoteEp)
        {
            if (_state != TcpState.Closed)
            {
                throw new Exception("Closed Only");
            }

            _state = TcpState.Connecting;
            _localEndPoint = null;
            _remoteEndPoint = remoteEp;

            _client = new TcpClient();
            _client.BeginConnect(remoteEp.Address, remoteEp.Port, ConnectCallback, null);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _client.EndConnect(ar);
                _localEndPoint = _client.Client.LocalEndPoint;
                _remoteEndPoint = _client.Client.RemoteEndPoint;
                _logger?.TraceFormat("Connected {0} with {1}", _remoteEndPoint, _localEndPoint);
            }
            catch (Exception e)
            {
                _state = TcpState.Closed;
                Closed?.Invoke(this, 0);
                _logger?.TraceFormat("Connect Failed {0}", e, _remoteEndPoint);
                return;
            }

            StartCommunication();
        }

        private void StartCommunication()
        {
            _state = TcpState.Connected;
            ProcessRecv();

            if (Connected != null)
                Connected(this);
        }

        public void Close()
        {
            _logger?.TraceFormat("Closed From {0}", _remoteEndPoint);

            if (_state == TcpState.Connected || _state == TcpState.Closing)
            {
                _client.Close();

                _state = TcpState.Closed;

                if (Closed != null)
                    Closed(this, 0);
            }
        }

        public void CloseSend()
        {
            if (_state == TcpState.Connected)
            {
                _state = TcpState.Closing;

                if (_sendPacketQueue.Count > 0)
                {
                    lock (_sendPacketQueue)
                        _sendPacketQueue.Add(null);
                }
                else
                {
                    lock (_sendPacketQueue)
                        _sendPacketQueue.Add(null);
                    ProcessSend();
                }
            }
        }

        public bool SendPacket(object packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }
            if (_state != TcpState.Connected)
            {
                return false;
            }

            lock (_sendPacketQueue)
                _sendPacketQueue.Add(packet);

            if (_sendProcessing == false)
                ProcessSend();

            return true;
        }

        private void ProcessSend()
        {
            lock (_sendPacketQueue)
            {
                while (_sendPacketQueue.Count > 0)
                {
                    // 큐에서 보낼 패킷을 얻어 바이트 스트림 구성

                    object packet;
                    {
                        packet = _sendPacketQueue[0];
                        _sendPacketQueue.RemoveAt(0);
                    }

                    if (packet == null)
                    {
                        _client.Client.Shutdown(SocketShutdown.Send);
                        break;
                    }

                    int length;
                    using (var ms = new MemoryStream())
                    {
                        _packetSerializer.Serialize(ms, packet);
                        length = (int)ms.Length;
                        if (length > _sendBuf.Length)
                        {
                            _logger?.ErrorFormat("ProcessSend got too large packet. Length={0}", length);
                            Close();
                            return;
                        }
                        Array.Copy(ms.GetBuffer(), _sendBuf, length);
                    }

                    // 전송

                    try
                    {
                        _sendProcessing = true;
                        _client.GetStream().BeginWrite(_sendBuf, 0, length, ProcessSendCallback, null);
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger?.Trace("ProcessSend Exception", e);
                        break;
                    }
                }
            }
        }

        private void ProcessSendCallback(IAsyncResult ar)
        {
            try
            {
                _client.GetStream().EndWrite(ar);
            }
            catch (Exception e)
            {
                _logger?.Info("ProcessSendCallback Exception", e);
                Close();
                return;
            }

            _sendProcessing = false;
            ProcessSend();
        }

        private void ProcessRecv()
        {
            try
            {
                _client.GetStream().BeginRead(
                    _recvBuf, _recvBufLen, _recvBuf.Length - _recvBufLen,
                    ProcessRecvCallback, null);
            }
            catch (Exception e)
            {
                _logger?.Info("ProcessRecv Exception", e);
                Close();
            }
        }

        private void ProcessRecvCallback(IAsyncResult ar)
        {
            try
            {
                int readed = _client.GetStream().EndRead(ar);
                if (readed == 0)
                {
                    _logger?.Info("ProcessRecvCallback readed == 0");
                    Close();
                    return;
                }
                _recvBufLen += readed;
            }
            catch (Exception e)
            {
                _logger?.Info("ProcessRecvCallback Exception", e);
                Close();
                return;
            }

            while (true)
            {
                using (var ms = new MemoryStream(_recvBuf, 0, _recvBufLen, false, true))
                {
                    var length = _packetSerializer.PeekLength(ms);
                    if (length > _recvBuf.Length)
                    {
                        _logger?.ErrorFormat("ProcessRecvCallback got too large packet. Length={0}", length);
                        Close();
                        return;
                    }
                    if (length == 0 || _recvBufLen < length)
                        break;

                    try
                    {
                        var packet = _packetSerializer.Deserialize(ms);
                        _lastReceiveTime = DateTime.UtcNow;
                        if (Received != null)
                            Received(this, packet);
                    }
                    catch (Exception e)
                    {
                        _logger?.Error("Deserialize Error", e);
                        Close();
                        return;
                    }

                    int leftLen = _recvBufLen - length;
                    if (leftLen > 0)
                    {
                        Array.Copy(_recvBuf, length, _recvBuf, 0, leftLen);
                        _recvBufLen = leftLen;
                    }
                    else
                    {
                        _recvBufLen = 0;
                    }
                }
            }

            ProcessRecv();
        }
    }
}
