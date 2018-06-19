﻿using System.ComponentModel;
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

        private ToolStripMenuItem MenuViewMessages;
        private ToolStripMenuItem MenuViewLastMessage;
        private ToolStripMenuItem MenuDeleteAllMessages;

        public AppContext()
        {
            CreateTrayIcon();
            InitServer();
            InitMainForm();
            EnableContextMenuItems();

            _messages.ListChanged += _messages_ListChanged;
        }

        private void _messages_ListChanged(object sender, ListChangedEventArgs e)
        {
            EnableContextMenuItems();
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

        private void CreateTrayIcon()
        {
            MenuViewMessages = new ToolStripMenuItem("View Messages", null, MenuViewMessages_Click)
            {
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.MenuFont, System.Drawing.FontStyle.Bold)
            };
            MenuViewLastMessage = new ToolStripMenuItem("View Last Message", null, MenuViewLastMessage_Click);
            MenuDeleteAllMessages = new ToolStripMenuItem("Delete All Messages", null, MenuDeleteAllMessages_Click);

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.AddRange(new[]
            {
                MenuViewMessages,
                MenuViewLastMessage,
                MenuDeleteAllMessages,
                new ToolStripMenuItem("Listen for Connections", null, MenuListenForConnections_Click),
                new ToolStripMenuItem("Options", null, MenuOptions_Click),
                new ToolStripMenuItem("Exit", null, MenuExit_Click)
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
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
