using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HACGUI.Main.TitleManager.Application.Tabs
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
