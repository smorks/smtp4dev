#region

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MimeKit;

#endregion

namespace Rnwood.Smtp4dev.MessageInspector
{
    /// <summary>
    /// Interaction logic for EmailInspectorWindow.xaml
    /// </summary>
    public partial class InspectorWindow : Window
    {
        private readonly MimeMessage _msg;

        public InspectorWindow(MimeMessage message)
        {
            InitializeComponent();
            _msg = message;
            Message = new MessageViewModel(message);
        }

        public MessageViewModel Message
        {
            get { return DataContext as MessageViewModel; }

            private set
            {
                DataContext = value;
            }
        }

        public PartViewModel SelectedPart
        {
            get { return treeView.SelectedItem as PartViewModel; }

            set { value.IsSelected = true; }
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem != null)
            {
                partDetailsGrid.IsEnabled = true;
            }
            else
            {
                partDetailsGrid.IsEnabled = false;
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            SelectedPart.View();
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SelectedPart.Save();
        }

        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached("Html",
            typeof (string), typeof (InspectorWindow), new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var newString = e.NewValue as string;

            var wb = d as WebBrowser;
            if (wb != null)
            {
                if (newString == null)
                {
                    wb.Navigate(new System.Uri("about:blank"));
                }
                else
                {
                    wb.NavigateToString(newString);
                }
            }
        }

        private void InspectorWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Message != null) { Message.SelectHtmlPart(); }

            HtmlView.IsSelected = true;
        }
    }
}