using Rnwood.SmtpServer;
using System;
using System.Threading;

namespace Rnwood.Smtp4dev
{
    internal sealed class ServerController
    {
        private Server _server;
        private Thread _t;

        public ServerController()
        {
            Behaviour = new ServerBehaviour();
        }

        public ServerBehaviour Behaviour { get; }

        public void Restart()
        {
            if (_server.IsRunning)
            {
                Stop();
                Start();
            }
        }

        public void Start()
        {
            _t = new Thread(ServerWork);
            _t.Start();
        }

        public void Stop()
        {
            if (_server.IsRunning)
            {
                _server.Stop();
                _t.Join();
            }
        }

        private void ServerWork()
        {
            try
            {
                _server = new Server(Behaviour);
                _server.Run();
            }
            catch (Exception exception)
            {
            }
        }
    }
}
