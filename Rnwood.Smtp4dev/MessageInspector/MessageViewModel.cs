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

        public void SmartSelect()
        {
            if (_main.Length > 0)
            {
                if (_main.Length == 1 && _main[0].Children == null)
                {
                    _main[0].IsSelected = true;
                }
                else
                {
                    FindHtmlPart(_main[0].Children);
                }
            }
        }

        private void FindHtmlPart(PartViewModel[] items)
        {
            if (items == null || items.Length == 0) return;
            
            foreach (var pvm in items)
            {
                if (pvm.IsHtml)
                {
                    pvm.IsSelected = true;
                    return;
                }
                FindHtmlPart(pvm.Children);
            }
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