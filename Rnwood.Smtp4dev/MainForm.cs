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

namespace Rnwood.Smtp4dev
{
    public partial class MainForm : Form
    {
        internal MainForm(ServerController server, BindingList<MessageViewModel> messages, BindingList<SessionViewModel> sessions)
        {
            InitializeComponent();

            Icon = Resources.ListeningIcon;

            Server = server;
            Server.ServerStarted += Server_ServerStarted;
            server.ServerStopped += Server_ServerStopped;

            Messages = messages;
            Messages.ListChanged += _messages_ListChanged;
            messageBindingSource.DataSource = Messages;

            Sessions = sessions;
            sessionBindingSource.DataSource = Sessions;
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
        }

        private void _messages_ListChanged(object sender, ListChangedEventArgs e)
        {
            deleteAllButton.Enabled = Messages.Count > 0;

            if (e.ListChangedType == ListChangedType.ItemAdded && Settings.Default.ScrollMessages &&
                messageGrid.RowCount > 0)
            {
                messageGrid.ClearSelection();
                messageGrid.Rows[messageGrid.RowCount - 1].Selected = true;
                messageGrid.FirstDisplayedScrollingRowIndex = messageGrid.RowCount - 1;
            }
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

        internal void ViewMessage(MessageViewModel message)
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

        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }

        private void StopServer()
        {
            Server.Stop();
        }

        private void EditOptions()
        {
            if (new OptionsForm().ShowDialog() == DialogResult.OK)
            {
                Server.Restart();
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

        internal void InspectMessage(MessageViewModel message)
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

        private void Server_ServerStopped(object sender, EventArgs e)
        {
            SetServerStoppedControls();
        }

        private void Server_ServerStarted(object sender, EventArgs e)
        {
            SetServerStartedControls();
        }

        private void SetServerStoppedControls()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(SetServerStoppedControls));
            }
            else
            {
                statusLabel.Text = "Not listening";
                runningPicture.Visible = stopListeningButton.Visible = false;
                notRunningPicture.Visible = startListeningButton.Visible = true;
            }
        }

        private void SetServerStartedControls()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(SetServerStartedControls));
            }
            else
            {
                statusLabel.Text = $"Listening on port {Settings.Default.PortNumber}";
                runningPicture.Visible = stopListeningButton.Visible = true;
                notRunningPicture.Visible = startListeningButton.Visible = false;
            }
        }
    }
}
