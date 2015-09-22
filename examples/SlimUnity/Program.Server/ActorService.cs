using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced.SlimSocketServer;
using Common.Logging;

namespace SlimUnity.Program.Server
{
    class ActorService
    {
        private ActorSystem _system;
        private TcpAcceptor _tcpAcceptor;
        private HashSet<ActorSession> _sessions = new HashSet<ActorSession>();
        private ILog _logger = LogManager.GetLogger("ActorService");

        public ActorService(ActorSystem system)
        {
            _system = system;
        }

        public void Start(IPEndPoint serviceEndPoint)
        {
            _logger.Info("Start");

            try
            {
                _tcpAcceptor = new TcpAcceptor();
                _tcpAcceptor.Accepted += OnAccept;
                _tcpAcceptor.Listen(serviceEndPoint);
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Start got exception.", e);
                return;
            }
        }

        public void Stop()
        {
            if (_tcpAcceptor == null)
                return;

            _tcpAcceptor.Close();
            _tcpAcceptor = null;
        }

        private TcpAcceptor.AcceptResult OnAccept(TcpAcceptor sender, Socket socket)
        {
            _logger.InfoFormat("OnAccept {0}", socket.RemoteEndPoint);

            var c = new TcpConnection(_logger);

            var a = _system.ActorOf(Props.Create<ActorSession>(this, c, socket));
            
            return TcpAcceptor.AcceptResult.Accept;
        }

        public void OnSessionCreated(ActorSession session)
        {
            _logger.InfoFormat("OnSessionCreated {0}", session);

            lock (_sessions)
                _sessions.Add(session);
        }

        public void OnSessionDestroyed(ActorSession session)
        {
            _logger.InfoFormat("OnSessionDestroyed {0}", session);

            lock (_sessions)
                _sessions.Remove(session);
        }
    }
}
