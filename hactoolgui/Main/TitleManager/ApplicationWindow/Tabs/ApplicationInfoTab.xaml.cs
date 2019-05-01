using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    /// <summary>
    /// Interaction logic for ApplicationInfoTab.xaml
    /// </summary>
    public partial class ApplicationInfoTab : UserControl
    {
        private ApplicationElement Element => ApplicationWindow.Current.Element;

        public string AppName => Element.Name;
        public string AppVersion => Element.FriendlyVersion;
        public string BcatPassphrase => Element.BcatPassphrase;
        public ulong TitleId => Element.TitleId;
        public ImageSource IconSource => Element.Icon;

        public ApplicationInfoTab()
        {
            InitializeComponent();
        }

        private void CopyImage(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(new WriteableBitmap((BitmapSource)Element.Icon));
        }
    }
}
