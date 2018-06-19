using Rnwood.SmtpServer;
using System;
using System.Threading;

namespace Rnwood.Smtp4dev
{
    internal sealed class ServerController
    {
        private Server _server;
        private Thread _t;

        public event EventHandler ServerStarted;
        public event EventHandler ServerStopped;

        public ServerController()
        {
            Behaviour = new ServerBehaviour();
        }

        public ServerBehaviour Behaviour { get; }

        public bool IsRunning
        {
            get
            {
                return _server != null ? _server.IsRunning : false;
            }
        }

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

            ServerStopped?.Invoke(this, EventArgs.Empty);
        }

        private void ServerWork()
        {
            ServerStarted?.Invoke(this, EventArgs.Empty);

            try
            {
                _server = new Server(Behaviour);
                _server.Run();
            }
            catch (Exception)
            {
            }
        }
    }
}
