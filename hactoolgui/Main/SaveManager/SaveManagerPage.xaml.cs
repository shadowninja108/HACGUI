using HACGUI.Extensions;
using HACGUI.Services;
using LibHac.IO.Save;
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

namespace HACGUI.Main.SaveManager
{
    /// <summary>
    /// Interaction logic for SaveManagerPage.xaml
    /// </summary>
    public partial class SaveManagerPage : PageExtension
    {
        public SaveManagerPage()
        {
            InitializeComponent();

            DeviceService.TitlesChanged += RefreshSavesView;

            DeviceService.Start();
        }

        private void SaveDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            SaveElement element = ListView.SelectedItem as SaveElement;
            ulong view = element.SaveId;
            if (view == 0)
                view = element.TitleId;
            MountService.Mount(new MountableFileSystem(element.Save, view.ToString("x16"), LibHac.IO.OpenMode.Read));
        }

        private void RefreshSavesView(Dictionary<ulong, LibHac.Application> apps, Dictionary<ulong, LibHac.Title> titles, Dictionary<string, LibHac.IO.Save.SaveDataFileSystem> saves)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ListView.Items.Clear();

                foreach (KeyValuePair<string, SaveDataFileSystem> kv in DeviceService.Saves)
                    ListView.Items.Add(new SaveElement(kv));
                
            }));
        }
    }
}
