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
        private ContextMenuStrip _contextMenu;

        private ToolStripMenuItem MenuViewMessages;
        private ToolStripMenuItem MenuViewLastMessage;
        private ToolStripMenuItem MenuDeleteAllMessages;
        private ToolStripMenuItem MenuListenForConnections;

        public AppContext()
        {
            CreateTrayIcon();
            InitServer();
            InitMainForm();
            EnableContextMenuItems();
            SetTrayIconText();

            _messages.ListChanged += _messages_ListChanged;

            if (Properties.Settings.Default.ListenOnStartup)
            {
                _server.Start();
            }
        }

        private void _messages_ListChanged(object sender, ListChangedEventArgs e)
        {
            EnableContextMenuItems();
            SetTrayIconText();
        }

        private void EnableContextMenuItems()
        {
            if (_messages.Count == 0)
            {
                MenuViewLastMessage.Enabled = false;
                MenuDeleteAllMessages.Enabled = false;
            }
            else
            {
                MenuViewLastMessage.Enabled = true;
                MenuDeleteAllMessages.Enabled = true;
            }
        }

        private void SetTrayIconText()
        {
            var listening = _server.IsRunning ? $"listening on :{Properties.Settings.Default.PortNumber}" : "not listening";
            _trayIcon.Text = $"{listening}\n{_messages.Count} messages";
        }

        private void CreateTrayIcon()
        {
            MenuViewMessages = new ToolStripMenuItem("View Messages", null, MenuViewMessages_Click)
            {
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.MenuFont, System.Drawing.FontStyle.Bold)
            };
            MenuViewLastMessage = new ToolStripMenuItem("View Last Message", null, MenuViewLastMessage_Click);
            MenuDeleteAllMessages = new ToolStripMenuItem("Delete All Messages", null, MenuDeleteAllMessages_Click);
            MenuListenForConnections = new ToolStripMenuItem("Listen for Connections", null, MenuListenForConnections_Click);

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.AddRange(new[]
            {
                MenuViewMessages,
                MenuViewLastMessage,
                MenuDeleteAllMessages,
                MenuListenForConnections,
                new ToolStripMenuItem("Options", null, MenuOptions_Click),
                new ToolStripMenuItem("Exit", null, MenuExit_Click)
            });

            _trayIcon = new NotifyIcon()
            {
                Text = "smtp4dev",
                ContextMenuStrip = _contextMenu,
                Icon = Properties.Resources.NotListeningIcon
            };

            _trayIcon.BalloonTipClicked += _trayIcon_BalloonTipClicked;
            _trayIcon.DoubleClick += _trayIcon_DoubleClick;

            _trayIcon.Visible = true;
        }

        private void InitServer()
        {
            _server = new ServerController();
            _server.ServerStarted += _server_ServerStarted;
            _server.ServerStopped += _server_ServerStopped;

        }

        private void _server_ServerStopped(object sender, System.EventArgs e)
        {
            SetTrayIconText();
            _trayIcon.Icon = Properties.Resources.NotListeningIcon;
            SetMenuListening(false);
        }

        private void _server_ServerStarted(object sender, System.EventArgs e)
        {
            SetTrayIconText();
            _trayIcon.Icon = Properties.Resources.ListeningIcon;
            SetMenuListening(true);
        }

        private void SetMenuListening(bool listening)
        {
            if (_contextMenu.InvokeRequired)
            {
                _contextMenu.Invoke(new System.Action(() => MenuListenForConnections.Checked = listening));
            }
            else
            {
                MenuListenForConnections.Checked = listening;
            }
        }

        private void InitMainForm()
        {
            _form = new MainForm(_server, _messages, _sessions);
        }


        private void _trayIcon_DoubleClick(object sender, System.EventArgs e)
        {
            _form.Show();
        }

        private void _trayIcon_BalloonTipClicked(object sender, System.EventArgs e)
        {
        }

        private void MenuViewMessages_Click(object sender, System.EventArgs e)
        {
            _form.Show();
        }

        private void MenuViewLastMessage_Click(object sender, System.EventArgs e)
        {
        }

        private void MenuDeleteAllMessages_Click(object sender, System.EventArgs e)
        {
            _messages.Clear();
            _sessions.Clear();
        }

        private void MenuListenForConnections_Click(object sender, System.EventArgs e)
        {
            if (_server.IsRunning)
            {
                _server.Stop();
            }
            else
            {
                _server.Start();
            }
        }

        private void MenuOptions_Click(object sender, System.EventArgs e)
        {
            if (new OptionsForm().ShowDialog() == DialogResult.OK)
            {
                _server.Restart();
            }
        }

        private void MenuExit_Click(object sender, System.EventArgs e)
        {
            if (_server.IsRunning)
            {
                _server.Stop();
            }
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
