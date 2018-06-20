using System;
using System.ComponentModel;
using System.Linq;
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

        private ToolStripMenuItem _menuViewMessages;
        private ToolStripMenuItem _menuViewLastMessage;
        private ToolStripMenuItem _menuDeleteAllMessages;
        private ToolStripMenuItem _menuListenForConnections;

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
                _menuViewLastMessage.Enabled = false;
                _menuDeleteAllMessages.Enabled = false;
            }
            else
            {
                _menuViewLastMessage.Enabled = true;
                _menuDeleteAllMessages.Enabled = true;
            }
        }

        private void SetTrayIconText()
        {
            var listening = _server.IsRunning ? $"listening on :{Properties.Settings.Default.PortNumber}" : "not listening";
            _trayIcon.Text = $"{listening}\n{_messages.Count} messages";
        }

        private void CreateTrayIcon()
        {
            _menuViewMessages = new ToolStripMenuItem("View Messages", null, MenuViewMessages_Click)
            {
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.MenuFont, System.Drawing.FontStyle.Bold)
            };
            _menuViewLastMessage = new ToolStripMenuItem("View Last Message", null, MenuViewLastMessage_Click);
            _menuDeleteAllMessages = new ToolStripMenuItem("Delete All Messages", null, MenuDeleteAllMessages_Click);
            _menuListenForConnections = new ToolStripMenuItem("Listen for Connections", null, MenuListenForConnections_Click);

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.AddRange(new[]
            {
                _menuViewMessages,
                _menuViewLastMessage,
                _menuDeleteAllMessages,
                _menuListenForConnections,
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
            _server.Behaviour.MessageReceived += _server_Behaviour_MessageReceived;
            _server.Behaviour.SessionCompleted += _server_Behaviour_SessionCompleted;
        }

        private void _server_Behaviour_SessionCompleted(object sender, SmtpServer.SessionEventArgs e)
        {
            _sessions.Add(new SessionViewModel(e.Session));
        }

        private void _server_Behaviour_MessageReceived(object sender, SmtpServer.MessageEventArgs e)
        {
            var message = new MessageViewModel(e.Message);

            _messages.Add(message);

            if (Properties.Settings.Default.MaxMessages > 0)
            {
                while (_messages.Count > Properties.Settings.Default.MaxMessages)
                {
                    _messages.RemoveAt(0);
                }
            }

            if (Properties.Settings.Default.AutoViewNewMessages ||
                Properties.Settings.Default.AutoInspectNewMessages)
            {
                if (Properties.Settings.Default.AutoViewNewMessages)
                {
                    _form.ViewMessage(message);
                }

                if (Properties.Settings.Default.AutoInspectNewMessages)
                {
                    _form.InspectMessage(message);
                }
            }
            else if (!_form.Visible && Properties.Settings.Default.BalloonNotifications)
            {
                string body = $"From: {message.From}\nTo: {message.To}\nSubject: {message.Subject}\n<Click here to view more details>";

                _trayIcon.ShowBalloonTip(3000, "Message Received", body, ToolTipIcon.Info);
            }

            if (_form.Visible && Properties.Settings.Default.BringToFrontOnNewMessage)
            {
                _form.BringToFront();
                _form.Activate();
            }
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
                _contextMenu.Invoke(new System.Action(() => _menuListenForConnections.Checked = listening));
            }
            else
            {
                _menuListenForConnections.Checked = listening;
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
            if (_messages.Count > 0)
            {
                if (Properties.Settings.Default.InspectOnBalloonClick)
                {
                    _form.InspectMessage(_messages.Last());
                }
                else
                {
                    _form.ViewMessage(_messages.Last());
                }
            }
            else
            {
                _form.Visible = true;
            }
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
