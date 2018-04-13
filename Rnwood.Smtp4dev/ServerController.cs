using Rnwood.SmtpServer;
using System;
using System.Threading;

namespace Rnwood.Smtp4dev
{
    internal sealed class ServerController
    {
        private Server _server;
        private Thread _t;

        public void Restart()
        {
            if (_server.IsRunning)
            {
                StopServer();
                StartServer();
            }
        }

        private void StartServer()
        {
            _t = new Thread(ServerWork);
            _t.Start();
        }

        private void StopServer()
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
                ServerBehaviour b = new ServerBehaviour();
                b.MessageReceived += OnMessageReceived;
                b.SessionCompleted += OnSessionCompleted;

                _server = new Server(b);
                _server.Run();
            }
            catch (Exception exception)
            {
            }
        }

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
        }
    }
}
