using HACGUI.Main.TitleManager.Application.Tabs.Extracts.Extractors;
using LibHac;
using LibHac.IO.NcaUtils;
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
using System.Windows.Shapes;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    /// <summary>
    /// Interaction logic for TitleInfoWindow.xaml
    /// </summary>
    public partial class TitleInfoWindow : Window
    {
        public TitleInfoWindow(TitleElement title)
        {
            InitializeComponent();

            foreach(NcaElement nca in title.Ncas)
                ListView.Items.Add(nca);
        }

        private void ExtractClicked(object sender, RoutedEventArgs e)
        {
            List<Nca> selected = new List<Nca>();
            foreach (NcaElement info in ListView.Items)
                if (info.Selected)
                    selected.Add(info.Nca);

            Window window = new ExtractPickerWindow(selected);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
    }
}
