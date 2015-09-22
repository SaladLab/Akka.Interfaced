using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Common.Logging;

namespace Akka.Interfaced.SlimSocketServer
{
    public class TcpConnection : IDisposable
    {
        private ILog _logger;

        private bool _isDisposed;
        private Socket _socket;
        private IPEndPoint _localEndPoint;
        private IPEndPoint _remoteEndPoint;
        private InterlockedCountFlag _issueCountFlag = new InterlockedCountFlag();
        private int _closeReason;

        private byte[] _receiveBuffer;
        private int _receiveLength;
        private ArraySegment<byte>? _receiveLargeBuffer;
        private int _receiveLargeLength;
        private SocketAsyncEventArgs _receiveArgs;
        private DateTime _lastReceiveTime;

        private byte[] _sendBuffer;
        private int _sendOffset;
        private int _sendLength;
        private ArraySegment<byte>? _sendLargeBuffer;
        private int _sendLargeOffset;
        private int _sendLargeLength;
        private SocketAsyncEventArgs _sendArgs;
        private int _sendCount;
        private ConcurrentQueue<object> _sendQueue;
        private bool _isSendShutdown;

        public bool Active
        {
            get { return _issueCountFlag.Flag == false; }
        }

        public Socket Socket
        {
            get { return _socket; }
        }

        public IPEndPoint LocalEndpoint
        {
            get { return _localEndPoint; }
        }

        public IPEndPoint RemoteEndpoint
        {
            get { return _remoteEndPoint; }
        }

        public TcpConnectionSettings Settings { get; set; }
        public int Id { get; set; }

        public DateTime LastReceiveTime
        {
            get { return _lastReceiveTime; }
        }

        public enum SerializeError
        {
            None,
            SerializeSizeExceeded,
            SerializeExceptionRaised,
            DeserializeSizeExceeded,
            DeserializeExceptionRaised,
            DeserializeNoPacket,
        }

        public event Action<TcpConnection> Opened;
        public event Action<TcpConnection, int> Closed;
        public event Action<TcpConnection, object> Received;
        public event Action<TcpConnection, SerializeError> SerializeErrored;

        public TcpConnection(ILog logger)
        {
            _logger = logger;
        }

        ~TcpConnection()
        {
            Dispose(false);
        }

        public void Open(Socket socket)
        {
            if (socket == null)
                throw new ArgumentException("socket");

            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (_socket != null)
                throw new InvalidOperationException("Already opened");

            if (Settings == null)
                throw new InvalidOperationException("Settings");

            socket.LingerState = new LingerOption(true, 0);
            _socket = socket;

            ProcessOpen();

            // 수신 시작

            var oldContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                IssueReceive();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        public void Close()
        {
            if (_issueCountFlag.Flag)
                return;

            if (_closeReason == 0)
                _closeReason = 1;

            if (_logger != null)
                _logger.Trace("Close connection");

            _socket.Close();
            if (_issueCountFlag.SetFlag())
                ProcessClose();
        }

        private void HandleSocketError(SocketError error)
        {
            if (_logger != null)
                _logger.TraceFormat("HandleSocketError: {0}", error);

            if (_closeReason == 0)
                _closeReason = (int)error;

            _socket.Close();
            if (_issueCountFlag.DecrementWithSetFlag())
                ProcessClose();
        }

        private void HandleSerializeError(SerializeError error)
        {
            if (_logger != null)
                _logger.WarnFormat("HandleSerializeError: {0}", error);

            if (SerializeErrored != null)
                SerializeErrored(this, error);

            if (_isSendShutdown == false)
                HandleSocketError(SocketError.NoData);
        }

        private void ProcessOpen()
        {
            _localEndPoint = (IPEndPoint)_socket.LocalEndPoint;
            _remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;

            // Initialize IO Buffers

            _receiveBuffer = new byte[Settings.ReceiveBufferSize];
            _receiveLength = 0;
            _receiveArgs = new SocketAsyncEventArgs();
            _receiveArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
            _receiveArgs.Completed += OnReceiveComplete;

            _sendBuffer = new byte[Settings.SendBufferSize];
            _sendLength = 0;
            _sendOffset = 0;
            _sendArgs = new SocketAsyncEventArgs();
            _sendArgs.SetBuffer(_sendBuffer, 0, _sendBuffer.Length);
            _sendArgs.Completed += OnSendComplete;
            _sendCount = 0;
            _sendQueue = new ConcurrentQueue<object>();

            // Opened Event

            if (Opened != null)
                Opened(this);
        }

        private void ProcessClose()
        {
            Debug.Assert(_issueCountFlag.Flag);

            // Closed Event

            if (Closed != null)
                Closed(this, _closeReason);

            // Dispose Resources

            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }

            if (_receiveArgs != null)
            {
                _receiveArgs.Dispose();
                _receiveArgs = null;
            }

            if (_sendArgs != null)
            {
                _sendArgs.Dispose();
                _sendArgs = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
                GC.SuppressFinalize(this);
            }

            _isDisposed = true;
        }

        private void IssueReceive()
        {
            if (!_issueCountFlag.Increment())
                return;

            if (_receiveLargeBuffer == null)
            {
                _receiveArgs.SetBuffer(
                    _receiveLength,
                    _receiveBuffer.Length - _receiveLength);
            }
            else
            {
                _receiveArgs.SetBuffer(
                    0,
                    _receiveBuffer.Length);
            }

            try
            {
                if (!_socket.ReceiveAsync(_receiveArgs))
                    OnReceiveComplete(_socket, _receiveArgs);
            }
            catch (SocketException e)
            {
                HandleSocketError(e.SocketErrorCode);
            }
            catch (ObjectDisposedException)
            {
                HandleSocketError(SocketError.NotConnected);
            }
        }

        private void OnReceiveComplete(object sender, SocketAsyncEventArgs args)
        {
            // 비동기 완료 및 재진입 처리

            if (args.SocketError != SocketError.Success)
            {
                HandleSocketError(args.SocketError);
                return;
            }

            var len = args.BytesTransferred;
            if (len == 0)
            {
                HandleSocketError(SocketError.Shutdown);
                return;
            }

            // Deserialize packet

            if (_receiveLargeBuffer == null)
            {
                if (TryDeserializeNormalPacket(len) == false)
                    return;
            }
            else
            {
                if (TryDeserializeLargePacket(len) == false)
                    return;
            }

            if (_issueCountFlag.Decrement())
            {
                ProcessClose();
                return;
            }

            IssueReceive();
        }

        private bool TryDeserializeNormalPacket(int len)
        {
            _receiveLength += len;

            var stream = new MemoryStream(_receiveBuffer, 0, _receiveLength, false, true);
            var readOffset = 0;
            while (true)
            {
                var packetLen = Settings.PacketSerializer.PeekLength(stream);
                if (packetLen == 0 || _receiveLength - readOffset < packetLen)
                {
                    // 패킷 크기가 최대 크기보다 큰지 확인
                    if (packetLen > Settings.ReceiveBufferMaxSize)
                    {
                        HandleSerializeError(SerializeError.DeserializeSizeExceeded);
                        return false;
                    }

                    _receiveLength -= readOffset;

                    if (packetLen > _receiveBuffer.Length)
                    {
                        // 패킷이 기본 버퍼보다 크기가 크면 Large 모드 전환

                        _receiveLargeBuffer = new ArraySegment<byte>(new byte[packetLen]);
                        _receiveLargeLength = _receiveLength;
                        Array.Copy(_receiveBuffer, readOffset,
                                   _receiveLargeBuffer.Value.Array,
                                   _receiveLargeBuffer.Value.Offset,
                                   _receiveLength);
                    }
                    else if (_receiveLength > 0)
                    {
                        // 버퍼에 남은 데이터가 있으면 맨 앞으로 이동

                        Array.Copy(_receiveBuffer, readOffset,
                                   _receiveBuffer, 0, _receiveLength);
                    }

                    break;
                }

                // 버퍼에서 패킷 재구성해내기

                object packet;
                try
                {
                    packet = Settings.PacketSerializer.Deserialize(stream);
                }
                catch (Exception e)
                {
                    if (_logger != null)
                    {
                        var bytes = Convert.ToBase64String(_receiveBuffer, 0, Math.Min(_receiveLength, 2048));
                        _logger.WarnFormat("Exception raised in deserializing: " + bytes, e);
                    }
                    HandleSerializeError(SerializeError.DeserializeExceptionRaised);
                    return false;
                }
                if (packet == null)
                {
                    HandleSerializeError(SerializeError.DeserializeNoPacket);
                    return false;
                }

                _lastReceiveTime = DateTime.UtcNow;
                if (Received != null)
                    Received(this, packet);

                readOffset += packetLen;
            }

            return true;
        }

        private bool TryDeserializeLargePacket(int len)
        {
            var leftLen = _receiveLargeBuffer.Value.Count - _receiveLargeLength;
            Array.Copy(_receiveBuffer, 0,
                       _receiveLargeBuffer.Value.Array,
                       _receiveLargeBuffer.Value.Offset + _receiveLargeLength,
                       Math.Min(leftLen, len));
            if (leftLen > len)
            {
                // 데이터를 더 받아야 한다
                _receiveLargeLength += len;
            }
            else
            {
                // 다 받았으니 Deserialize

                var stream = new MemoryStream(
                    _receiveLargeBuffer.Value.Array,
                    _receiveLargeBuffer.Value.Offset,
                    _receiveLargeBuffer.Value.Count,
                    false, true);

                object packet;
                try
                {
                    packet = Settings.PacketSerializer.Deserialize(stream);
                }
                catch (Exception e)
                {
                    if (_logger != null)
                    {
                        var bytes = Convert.ToBase64String(_receiveLargeBuffer.Value.Array,
                                                           _receiveLargeBuffer.Value.Offset,
                                                           Math.Min(_receiveLargeBuffer.Value.Count, 2048));
                        _logger.WarnFormat("Exception raised in deserializing: " + bytes, e);
                    }
                    HandleSerializeError(SerializeError.DeserializeExceptionRaised);
                    return false;
                }
                if (packet == null)
                {
                    HandleSerializeError(SerializeError.DeserializeNoPacket);
                    return false;
                }

                _lastReceiveTime = DateTime.UtcNow;
                if (Received != null)
                    Received(this, packet);

                var extraLen = len - leftLen;
                if (extraLen > 0)
                {
                    Array.Copy(_receiveBuffer, leftLen,
                               _receiveBuffer, 0, extraLen);
                }
                _receiveLength = extraLen;
                _receiveLargeBuffer = null;
            }

            return true;
        }

        public void Send(object packet)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");
            if (_isSendShutdown)
                return;

            if (Interlocked.Increment(ref _sendCount) == 1)
            {
                var oldContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(null);

                try
                {
                    StartSend(packet);
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(oldContext);
                }
            }
            else
            {
                _sendQueue.Enqueue(packet);
            }
        }

        private void StartSend(object packet)
        {
            if (!_issueCountFlag.Increment())
                return;

            var packetLen = Settings.PacketSerializer.EstimateLength(packet);
            var tailSize = 0;
            if (packetLen > _sendBuffer.Length)
                tailSize = packetLen - _sendBuffer.Length;

            var stream = new HeadTailWriteStream(new ArraySegment<byte>(_sendBuffer), tailSize);
            try
            {
                Settings.PacketSerializer.Serialize(stream, packet);
            }
            catch (Exception e)
            {
                if (_logger != null)
                    _logger.WarnFormat("Exception raised in serializing", e);
                HandleSerializeError(SerializeError.SerializeExceptionRaised);
                return;
            }

            var streamLength = (int)stream.Length;
            if (streamLength <= _sendBuffer.Length)
            {
                // 전송 버퍼안으로 보낼 수 있으므로 Normal 모드

                _sendOffset = 0;
                _sendLength = streamLength;
            }
            else
            {
                // 패킷 크기가 최대 크기보다 큰지 확인

                if (streamLength > Settings.SendBufferMaxSize)
                {
                    HandleSerializeError(SerializeError.SerializeSizeExceeded);
                    return;
                }

                // 전송 버퍼를 넘어서는 크기이므로 Large 모드로 전환

                _sendOffset = 0;
                _sendLength = _sendBuffer.Length;

                _sendLargeBuffer = stream.Tail;
                _sendLargeOffset = 0;
                _sendLargeLength = streamLength - _sendBuffer.Length;

                Debug.Assert(_sendLargeBuffer != null && _sendLargeLength > 0);
            }

            IssueSend();
        }

        private void IssueSend()
        {
            _sendArgs.SetBuffer(_sendOffset, _sendLength - _sendOffset);
            try
            {
                if (!_socket.SendAsync(_sendArgs))
                    OnSendComplete(_socket, _sendArgs);
            }
            catch (SocketException e)
            {
                HandleSocketError(e.SocketErrorCode);
            }
            catch (ObjectDisposedException)
            {
                HandleSocketError(SocketError.NotConnected);
            }
        }

        private void OnSendComplete(object sender, SocketAsyncEventArgs args)
        {
            // 비동기 완료 및 재진입 처리

            if (args.SocketError != SocketError.Success)
            {
                HandleSocketError(args.SocketError);
                return;
            }

            if (_issueCountFlag.Decrement())
            {
                ProcessClose();
                return;
            }

            _sendOffset += args.BytesTransferred;
            if (_sendOffset < _sendLength)
            {
                // Send 버퍼를 덜 보냈으면 나머지를 다시 전송 요청

                if (!_issueCountFlag.Increment())
                    return;

                IssueSend();
            }
            else if (_sendLargeBuffer != null)
            {
                // Large 모드일 때 Large 버퍼의 내용을 Send 버퍼에 복사해 전송 요청

                if (!_issueCountFlag.Increment())
                    return;

                int len = Math.Min(_sendLargeLength - _sendLargeOffset, _sendBuffer.Length);
                Debug.Assert(len > 0);

                Array.Copy(
                    _sendLargeBuffer.Value.Array,
                    _sendLargeBuffer.Value.Offset + _sendLargeOffset,
                    _sendBuffer, 0, len);
                _sendLargeOffset += len;
                if (_sendLargeOffset == _sendLargeLength)
                    _sendLargeBuffer = null;

                _sendLength = len;
                _sendOffset = 0;

                IssueSend();
            }
            else
            {
                // 전송 패킷 큐에 대기중인 패킷이 있으면 꺼내서 전송

                if (Interlocked.Decrement(ref _sendCount) > 0)
                {
                    object packet;
                    while (_sendQueue.TryDequeue(out packet) == false)
                    {
                    }
                    if (packet != null)
                        StartSend(packet);
                    else
                        Close();
                }
            }
        }

        public void FlushAndClose()
        {
            if (Active == false || _isSendShutdown)
                return;

            Volatile.Write(ref _isSendShutdown, true);

            // Shutdown 시도

            if (Interlocked.Increment(ref _sendCount) == 1)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }
            else
            {
                _sendQueue.Enqueue(null);
            }
        }
    }
}
