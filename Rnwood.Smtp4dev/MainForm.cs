using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using Rnwood.Smtp4dev.MessageInspector;
using Rnwood.Smtp4dev.Properties;
using Rnwood.SmtpServer;

namespace Rnwood.Smtp4dev
{
    public partial class MainForm : Form
    {
        internal MainForm(ServerController server, BindingList<MessageViewModel> messages, BindingList<SessionViewModel> sessions)
        {
            InitializeComponent();

            Icon = Resources.ListeningIcon;

            Server = server;

            Messages = messages;
            Messages.ListChanged += _messages_ListChanged;
            messageBindingSource.DataSource = Messages;

            Sessions = sessions;
            sessionBindingSource.DataSource = Sessions;

            InitServer();
        }

        public BindingList<MessageViewModel> Messages { get; }

        public BindingList<SessionViewModel> Sessions { get; }

        internal ServerController Server { get; }

        public MessageViewModel SelectedMessage
        {
            get
            {
                if (messageGrid.SelectedRows.Count != 1)
                {
                    return null;
                }

                return
                    messageGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => (MessageViewModel)row.DataBoundItem)
                        .Single();
            }
        }


        public MessageViewModel[] SelectedMessages
        {
            get
            {
                return
                    messageGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => (MessageViewModel)row.DataBoundItem)
                        .ToArray();
            }
        }

        public SessionViewModel SelectedSession
        {
            get
            {
                return
                    (SessionViewModel)
                    sessionsGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => row.DataBoundItem).FirstOrDefault();
            }
        }

        public SessionViewModel[] SelectedSessions
        {
            get
            {
                return
                    sessionsGrid.SelectedRows.Cast<DataGridViewRow>().Select(row => (SessionViewModel)row.DataBoundItem)
                        .ToArray();
            }
        }

        private void StartServer()
        {
            Server.Start();

            trayIcon.Text = string.Format("smtp4dev (listening on :{0})\n{1} messages", Settings.Default.PortNumber, Messages.Count);
            trayIcon.Icon = Resources.ListeningIcon;
            listenForConnectionsToolStripMenuItem.Checked = true;
            statusLabel.Text = string.Format("Listening on port {0}", Settings.Default.PortNumber);
            runningPicture.Visible = stopListeningButton.Visible = true;
            notRunningPicture.Visible = startListeningButton.Visible = false;
        }

        private void _messages_ListChanged(object sender, ListChangedEventArgs e)
        {
            deleteAllMenuItem.Enabled = deleteAllButton.Enabled = viewLastMessageMenuItem.Enabled = Messages.Count > 0;
            trayIcon.Text = string.Format("smtp4dev (listening on :{0})\n{1} messages", Settings.Default.PortNumber, Messages.Count);

            if (e.ListChangedType == ListChangedType.ItemAdded && Settings.Default.ScrollMessages &&
                messageGrid.RowCount > 0)
            {
                messageGrid.ClearSelection();
                messageGrid.Rows[messageGrid.RowCount - 1].Selected = true;
                messageGrid.FirstDisplayedScrollingRowIndex = messageGrid.RowCount - 1;
            }
        }

        private void InitServer()
        {
            Server.Behaviour.MessageReceived += OnMessageReceived;
            Server.Behaviour.SessionCompleted += OnSessionCompleted;
        }

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            Invoke(new Action(() => { Sessions.Add(new SessionViewModel(e.Session)); }));
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            MessageViewModel message = new MessageViewModel(e.Message);

            Invoke(new Action(() =>
            {
                Messages.Add(message);

                if (Settings.Default.MaxMessages > 0)
                {
                    while (Messages.Count > Settings.Default.MaxMessages)
                    {
                        Messages.RemoveAt(0);
                    }
                }

                if (Settings.Default.AutoViewNewMessages ||
                    Settings.Default.AutoInspectNewMessages)
                {
                    if (Settings.Default.AutoViewNewMessages)
                    {
                        ViewMessage(message);
                    }

                    if (Settings.Default.AutoInspectNewMessages)
                    {
                        InspectMessage(message);
                    }
                }
                else if (!Visible && Settings.Default.BalloonNotifications)
                {
                    string body =
                        string.Format(
                            "From: {0}\nTo: {1}\nSubject: {2}\n<Click here to view more details>",
                            message.From,
                            message.To,
                            message.Subject);

                    trayIcon.ShowBalloonTip(3000, "Message Received", body, ToolTipIcon.Info);
                }

                if (Visible && Settings.Default.BringToFrontOnNewMessage)
                {
                    BringToFront();
                    Activate();
                }
            }));
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Quit();
        }

        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            Visible = true;
        }

        private void viewButton_Click(object sender, EventArgs e)
        {
            ViewSelectedMessages();
        }

        private void ViewSelectedMessages()
        {
            foreach (MessageViewModel message in SelectedMessages)
            {
                ViewMessage(message);
            }
        }

        private void ViewMessage(MessageViewModel message)
        {
            if (Settings.Default.UseMessageInspectorOnDoubleClick)
            {
                InspectMessage(message);
                return;
            }


            TempFileCollection tempFiles = new TempFileCollection();
            FileInfo msgFile = new FileInfo(tempFiles.AddExtension("eml"));
            message.SaveToFile(msgFile);

            if (Registry.ClassesRoot.OpenSubKey(".eml", false) == null || string.IsNullOrEmpty((string)Registry.ClassesRoot.OpenSubKey(".eml", false).GetValue(null)))
            {
                switch (MessageBox.Show(this,
                                        "You don't appear to have a viewer application associated with .eml files!\nWould you like to download Windows Live Mail (free from live.com website)?",
                                        "View Message", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        Process.Start("http://download.live.com/wlmail");
                        return;
                    case DialogResult.Cancel:
                        return;
                }
            }

            Process.Start(msgFile.FullName);
            messageGrid.Refresh();
        }

        private void deleteAllButton_Click(object sender, EventArgs e)
        {
            DeleteAllMessages();
        }

        private void DeleteAllMessages()
        {
            Messages.Clear();
            Sessions.Clear();
        }

        private void messageGrid_DoubleClick(object sender, EventArgs e)
        {
            ViewSelectedMessages();
        }

        private void clearAllEmailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteAllMessages();
        }

        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            trayIcon.Visible = !Visible;

            if (Visible)
            {
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void StopServer()
        {
            Server.Stop();

            trayIcon.Icon = Resources.NotListeningIcon;
            listenForConnectionsToolStripMenuItem.Checked = false;
            statusLabel.Text = "Not listening";
            runningPicture.Visible = stopListeningButton.Visible = false;
            notRunningPicture.Visible = startListeningButton.Visible = true;
            trayIcon.Text = string.Format("smtp4dev (not listening)\n{0} messages", Messages.Count);
        }

        private void viewLastMessageMenuItem_Click(object sender, EventArgs e)
        {
            ViewMessage(Messages.Last());
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditOptions();
        }

        private void EditOptions()
        {
            if (new OptionsForm().ShowDialog() == DialogResult.OK)
            {
                Server.Restart();
            }
        }

        private void trayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (Messages.Count > 0)
            {
                if (Settings.Default.InspectOnBalloonClick)
                {
                    InspectMessage(Messages.Last());
                }
                else
                {
                    ViewMessage(Messages.Last());
                }
            }
            else
            {
                Visible = true;
            }
        }

        private void optionsButton_Click(object sender, EventArgs e)
        {
            EditOptions();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            foreach (MessageViewModel message in SelectedMessages)
            {
                if (saveMessageFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        message.SaveToFile(new FileInfo(saveMessageFileDialog.FileName));
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(string.Format("Failed to save: {0}", ex.Message), "Error", MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            foreach (MessageViewModel message in SelectedMessages)
            {
                Messages.Remove(message);
            }

            foreach (SessionViewModel session in Sessions.Where(s => !Messages.Any(mvm => s.Session.Messages.Contains(mvm.Message))).ToArray())
            {
                Sessions.Remove(session);
            }
        }

        private void stopListeningButton_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void startListeningButton_Click(object sender, EventArgs e)
        {
            StartServer();
        }


        private void listenForConnectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listenForConnectionsToolStripMenuItem.Checked)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }
        }

        private void messageGrid_SelectionChanged(object sender, EventArgs e)
        {
            inspectMessageButton.Enabled =
                deleteButton.Enabled = viewButton.Enabled = saveButton.Enabled = SelectedMessages.Length > 0;
        }

        private void messageGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                MessageViewModel message = (MessageViewModel)messageGrid.Rows[e.RowIndex].DataBoundItem;

                if (!message.HasBeenViewed)
                {
                    e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                }
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && Properties.Settings.Default.MinimizeToSysTray)
            {
                Visible = false;
            }
        }

        private void inspectButton_Click(object sender, EventArgs e)
        {
            foreach (MessageViewModel message in SelectedMessages)
            {
                InspectMessage(message);
            }
        }

        private void InspectMessage(MessageViewModel message)
        {
            message.MarkAsViewed();

            InspectorWindow form = new InspectorWindow(message.Parts);
            form.Show();

            messageGrid.Refresh();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            foreach (SessionViewModel session in SelectedSessions)
            {
                session.ViewLog();
            }
        }

        private void sessionsGrid_SelectionChanged(object sender, EventArgs e)
        {
            viewSessionButton.Enabled = deleteSessionButton.Enabled = SelectedSessions.Length > 0;
        }

        private void deleteSessionButton_Click(object sender, EventArgs e)
        {
            foreach (SessionViewModel session in SelectedSessions)
            {
                Sessions.Remove(session);

                foreach (MessageViewModel message in Messages.Where(mvm => session.Session.Messages.Any(m => mvm.Message == m)).ToArray())
                {
                    Messages.Remove(message);
                }
            }
        }
    }
}
