using System.ComponentModel;
using System.Windows.Forms;

namespace Rnwood.Smtp4dev
{
    internal sealed class AppContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;

        private readonly BindingList<MessageViewModel> _messages = new BindingList<MessageViewModel>();
        private readonly BindingList<SessionViewModel> _sessions = new BindingList<SessionViewModel>();
        private ServerController _server;
        private MainForm _form;

        public AppContext()
        {
            CreateTrayIcon();
            InitServer();
            InitMainForm();
        }

        private void CreateTrayIcon()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.AddRange(new[]
            {
                new ToolStripMenuItem("View Messages", null, Menu_ViewMessages),
                new ToolStripMenuItem("View Last Message", null, Menu_ViewLastMessage),
                new ToolStripMenuItem("Delete All Messages", null, Menu_DeleteAllMessages),
                new ToolStripMenuItem("Listen for Connections", null, Menu_ListenForConnections),
                new ToolStripMenuItem("Options", null, Menu_Options),
                new ToolStripMenuItem("Exit", null, Menu_Exit)
            });

            _trayIcon = new NotifyIcon()
            {
                Text = "smtp4dev",
                ContextMenuStrip = contextMenu,
                Icon = Properties.Resources.ListeningIcon
            };

            _trayIcon.BalloonTipClicked += _trayIcon_BalloonTipClicked;
            _trayIcon.DoubleClick += _trayIcon_DoubleClick;

            _trayIcon.Visible = true;
        }

        private void InitServer()
        {
            _server = new ServerController();

        }

        private void InitMainForm()
        {
            _form = new MainForm(_server, _messages, _sessions);
        }


        private void _trayIcon_DoubleClick(object sender, System.EventArgs e)
        {
        }

        private void _trayIcon_BalloonTipClicked(object sender, System.EventArgs e)
        {
        }

        private void Menu_ViewMessages(object sender, System.EventArgs e)
        {
        }

        private void Menu_ViewLastMessage(object sender, System.EventArgs e)
        {
        }

        private void Menu_DeleteAllMessages(object sender, System.EventArgs e)
        {
            _messages.Clear();
            _sessions.Clear();
        }

        private void Menu_ListenForConnections(object sender, System.EventArgs e)
        {
        }

        private void Menu_Options(object sender, System.EventArgs e)
        {
        }

        private void Menu_Exit(object sender, System.EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
