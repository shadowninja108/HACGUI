using HACGUI.Extensions;
using HACGUI.Services;
using LibHac;
using LibHac.IO;
using LibHac.IO.Save;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static HACGUI.Main.TitleManager.FSView;
using Application = LibHac.Application;
using Image = System.Windows.Controls.Image;

namespace HACGUI.Main.TitleManager
{
    /// <summary>
    /// Interaction logic for MainTitleManagerPage.xaml
    /// </summary>
    public partial class MainTitleManagerPage : PageExtension
    {
        public MainTitleManagerPage()
        {
            InitializeComponent();

            DeviceService.TitlesChanged += RefreshListView;
        }

        public void RefreshListView(Dictionary<ulong, LibHac.Application> apps, Dictionary<ulong, Title> titles, Dictionary<string, SaveDataFileSystem> saves)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                ListView.Items.Clear();

                foreach (ApplicationElement element in DeviceService.ApplicationElements)
                {
                    element.Load();
                    ListView.Items.Add(element);
                }
            }));
        }

        private void ApplicationDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ApplicationElement element = ((ApplicationElement)((ListView)sender).SelectedItem);

            if (element != null)
            {
                Application.ApplicationWindow window = new Application.ApplicationWindow(element)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
        }

    }
}
