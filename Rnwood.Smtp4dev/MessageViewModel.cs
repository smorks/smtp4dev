#region

using System;
using System.IO;
using Rnwood.SmtpServer;
using MimeKit;

#endregion

namespace Rnwood.Smtp4dev
{
    public class MessageViewModel
    {
        public MessageViewModel(Message message)
        {
            Message = message;
        }

        public Message Message { get; private set; }

        public string From
        {
            get { return Message.From; }
        }

        public string To
        {
            get { return string.Join(", ", Message.To); }
        }

        public DateTime ReceivedDate
        {
            get { return Message.ReceivedDate; }
        }

        public string Subject
        {
            get { return Parts.Subject; }
        }

        private MimeMessage _contents;
        public MimeMessage Parts
        {
            get
            {
                if (_contents == null)
                {
                    _contents = MimeMessage.Load(Message.GetData());
                }

                return _contents;
            }
        }

        public bool HasBeenViewed { get; private set; }

        public void SaveToFile(FileInfo file)
        {
            HasBeenViewed = true;

            byte[] data = new byte[64 * 1024];
            int bytesRead;

            using (Stream dataStream = Message.GetData(false))
            {
                using (FileStream fileStream = file.OpenWrite())
                {
                    while ((bytesRead = dataStream.Read(data, 0, data.Length)) > 0)
                    {
                        fileStream.Write(data, 0, bytesRead);
                    }
                }
            }
        }

        public void MarkAsViewed()
        {
            HasBeenViewed = true;
        }
    }
}