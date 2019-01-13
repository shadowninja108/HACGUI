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
        //private LibHac.Application Application => ApplicationWindow.Current.Applications[Element.BaseTitleId];

        public ApplicationInfoTab()
        {
            InitializeComponent();

            Icon.Source = Element.Icon;
            NameBox.Text = Element.Name;
            TitleIDBox.Text = string.Format("{0:x16}", Element.TitleId);
            BCATPassphraseBox.Text = Element.BcatPassphrase;
            VersionBox.Text = Element.FriendlyVersion;
        }

        private void CopyImage(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(new WriteableBitmap((BitmapSource)Element.Icon));
        }
    }
}
