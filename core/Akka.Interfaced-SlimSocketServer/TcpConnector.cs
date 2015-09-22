using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Akka.Interfaced.SlimSocketServer
{
    public class TcpConnector
    {
        private Socket _socket;

        public event Action<TcpConnector, Socket> Connected;
        public event Action<TcpConnector, SocketError> ConnectFailed;

        private TaskCompletionSource<Socket> _connectTcs;

        public void Connect(IPEndPoint remoteEndPoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connectArgs = new SocketAsyncEventArgs();
            connectArgs.RemoteEndPoint = remoteEndPoint;
            connectArgs.Completed += OnConnectComplete;

            try
            {
                if (!_socket.ConnectAsync(connectArgs))
                    OnConnectComplete(_socket, connectArgs);
            }
            catch (SocketException e)
            {
                HandleSocketError(e.SocketErrorCode);
            }
        }

        public Task<Socket> ConnectAsync(IPEndPoint remoteEndPoint)
        {
            _connectTcs = new TaskCompletionSource<Socket>();
            var task = _connectTcs.Task;
            Connect(remoteEndPoint);
            return task;
        }

        private void OnConnectComplete(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                HandleSocketError(args.SocketError);
                return;
            }

            var socket = _socket;
            _socket = null;
            if (Connected != null)
                Connected(this, socket);

            if (_connectTcs != null)
            {
                _connectTcs.SetResult(socket);
                _connectTcs = null;
            }
        }

        private void HandleSocketError(SocketError error)
        {
            if (ConnectFailed != null)
                ConnectFailed(this, error);

            if (_connectTcs != null)
            {
                _connectTcs.SetResult(null);
                _connectTcs = null;
            }
        }
    }
}
