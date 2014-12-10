using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using MimeKit;

namespace Rnwood.Smtp4dev.MessageInspector
{
    public class PartViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly MimeEntity _part;

        private bool _isSelected;
        private bool _isExpanded;

        private List<PartViewModel> _parts;

        public PartViewModel(MimeEntity part)
        {
            _part = part;
            var mp = _part as Multipart;
            if (mp != null) { IsExpanded = true; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }

            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                _isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        public bool IsHtml
        {
            get
            {
                var tp = _part as TextPart;
                if (tp != null)
                {
                    return tp.IsHtml;
                }
                return false;
            }
        }

        public PartViewModel[] Children
        {
            get
            {
                if (_parts == null)
                {
                    var mp = _part as Multipart;
                    if (mp != null)
                    {
                        _parts = new List<PartViewModel>(mp.Select(p => new PartViewModel(p)));
                        return _parts.ToArray();
                    }
                }
                else
                {
                    return _parts.ToArray();
                }
                
                return null;
            }
        }

        public HeaderViewModel[] Headers
        {
            get
            {
                return _part.Headers.Select(hdr => new HeaderViewModel(hdr.Id.ToHeaderName(), hdr.Value)).ToArray();
            }
        }

        public string Data
        {
            get
            {
                return _part.ToString();
            }
        }

        public string Body
        {
            get
            {
                var tp = _part as MimeKit.TextPart;
                if (tp != null)
                {
                    return tp.Text;
                }
                return null;
            }
        }

        protected string MimeType
        {
            get { return _part.ContentType.MimeType; }
        }

        public string Name
        {
            get
            {
                var mp = _part as MimePart;
                if (mp != null)
                {
                    if (mp.ContentDisposition != null)
                    {
                        return mp.FileName ??
                               "Unnamed" + ": " + MimeType + " (" + _part.ContentDisposition.Size + " bytes)";
                    }
                }                 
                return "Unnamed: " + MimeType;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Save()
        {
            var mp = _part as MimePart;
            if (mp != null)
            {
                var dialog = new SaveFileDialog();

                string filename = (mp.FileName ?? "Unnamed");

                if (string.IsNullOrEmpty(Path.GetExtension(filename)))
                {
                    filename = filename + (MIMEDatabase.GetExtension(MimeType));
                }

                dialog.FileName = filename;
                dialog.Filter = "File (*.*)|*.*";

                if (dialog.ShowDialog() == true)
                {
                    using (FileStream stream = File.OpenWrite(dialog.FileName))
                    {
                        mp.ContentObject.DecodeTo(stream);
                    }
                }
            }
        }

        public void View()
        {
            var mp = _part as MimePart;
            if (mp != null)
            {
                string extn = Path.GetExtension(mp.FileName ?? "Unnamed");

                if (string.IsNullOrEmpty(extn))
                {
                    extn = MIMEDatabase.GetExtension(MimeType) ?? ".part";
                }

                var tempFiles = new TempFileCollection();
                var msgFile = new FileInfo(tempFiles.AddExtension(extn.TrimStart('.')));

                using (FileStream stream = msgFile.OpenWrite())
                {
                    mp.ContentObject.DecodeTo(stream);
                }

                Process.Start(msgFile.FullName);
            }
        }
    }
}
