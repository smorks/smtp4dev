using Microsoft.Win32;
using Rnwood.Smtp4dev.MessageInspector;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
                    ViewMessage(message);
                }

                if (Properties.Settings.Default.AutoInspectNewMessages)
                {
                    InspectMessage(message);
                }
            }
            else if (_form == null && Properties.Settings.Default.BalloonNotifications)
            {
                string body = $"From: {message.From}\nTo: {message.To}\nSubject: {message.Subject}\n<Click here to view more details>";

                _trayIcon.ShowBalloonTip(3000, "Message Received", body, ToolTipIcon.Info);
            }

            if (_form != null && Properties.Settings.Default.BringToFrontOnNewMessage)
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
                _contextMenu.Invoke(new Action(() => _menuListenForConnections.Checked = listening));
            }
            else
            {
                _menuListenForConnections.Checked = listening;
            }
        }

        private void _trayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainForm();
        }

        private void _trayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (_messages.Count > 0)
            {
                if (Properties.Settings.Default.InspectOnBalloonClick)
                {
                    InspectMessage(_messages.Last());
                }
                else
                {
                    ViewMessage(_messages.Last());
                }
            }
            else
            {
                ShowMainForm();
            }
        }

        private void MenuViewMessages_Click(object sender, System.EventArgs e)
        {
            ShowMainForm();
        }

        private void MenuViewLastMessage_Click(object sender, System.EventArgs e)
        {
            if (_messages.Count > 0)
            {
                ViewOrInspectMessage(_messages.Last());
            }
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
            if (_form != null)
            {
                _form.Close();
            }

            if (_server.IsRunning)
            {
                _server.Stop();
            }

            _trayIcon.Visible = false;
            Application.Exit();
        }

        private void ShowMainForm()
        {
            if (_form == null)
            {
                _form = new MainForm(_server, _messages, _sessions);
                _form.InspectMessageClicked += _form_InspectMessageClicked;
                _form.ViewMessageClicked += _form_ViewMessageClicked;
                _form.ViewOrInspectMessagedClicked += _form_ViewOrInspectMessagedClicked;
                _form.FormClosed += _form_FormClosed;
                _form.Show();
            }
            else
            {
                _form.Activate();
            }
        }

        private void _form_ViewOrInspectMessagedClicked(object sender, MessageViewModel msg)
        {
            ViewOrInspectMessage(msg);
        }

        private void _form_ViewMessageClicked(object sender, MessageViewModel msg)
        {
            ViewMessage(msg);
        }

        private void _form_InspectMessageClicked(object sender, MessageViewModel msg)
        {
            InspectMessage(msg);
        }

        private void _form_FormClosed(object sender, FormClosedEventArgs e)
        {
            _form = null;
        }

        private void ViewOrInspectMessage(MessageViewModel msg)
        {
            if (Properties.Settings.Default.UseMessageInspectorOnDoubleClick)
            {
                InspectMessage(msg);
            }
            else
            {
                ViewMessage(msg);
            }
        }

        private void InspectMessage(MessageViewModel message)
        {
            message.MarkAsViewed();

            InspectorWindow form = new InspectorWindow(message.Parts);
            form.Show();
        }

        internal void ViewMessage(MessageViewModel message)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".eml");
            var msgFile = new FileInfo(tempFile);
            message.SaveToFile(msgFile);

            if (Registry.ClassesRoot.OpenSubKey(".eml", false) == null || string.IsNullOrEmpty((string)Registry.ClassesRoot.OpenSubKey(".eml", false).GetValue(null)))
            {
                switch (MessageBox.Show("You don't appear to have a viewer application associated with .eml files!\nWould you like to download Windows Live Mail (free from live.com website)?",
                                        "View Message", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        Process.Start("http://download.live.com/wlmail");
                        return;
                    case DialogResult.Cancel:
                        return;
                }
            }

            var p = new Process()
            {
                StartInfo = { FileName = msgFile.FullName },
                EnableRaisingEvents = true
            };

            p.Exited += (sender, e) =>
            {
                try
                {
                    File.Delete(msgFile.FullName);
                }
                catch
                {
                }
            };

            p.Start();
        }
    }
}
