using System.ComponentModel;
using MimeKit;

namespace Rnwood.Smtp4dev.MessageInspector
{
    public class MessageViewModel : INotifyPropertyChanged
    {
        private readonly MimeMessage _msg;
        private readonly PartViewModel[] _main;

        public MessageViewModel(MimeMessage message)
        {
            _msg = message;
            var pvm = new PartViewModel(message.Body);
            pvm.IsExpanded = true;
            pvm.IsSelected = true;
            _main = new[] {pvm};
        }

        public PartViewModel[] MainParts
        {
            get { return _main; }
        }

        public string Data
        {
            get { return _msg.ToString(); }
        }

        public string Subject
        {
            get { return _msg.Subject; }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}