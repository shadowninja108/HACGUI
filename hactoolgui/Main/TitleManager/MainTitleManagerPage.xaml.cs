using HACGUI.Extensions;
using HACGUI.Services;
using LibHac;
using LibHac.Fs.Save;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
                    element.Load(ListView);
                    ListView.Items.Add(element);
                }
            }));
        }

        private void ApplicationDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ApplicationElement element = ((ApplicationElement)((ListView)sender).SelectedItem);

            if (element != null)
            {
                ApplicationWindow.ApplicationWindow window = new ApplicationWindow.ApplicationWindow(element)
                {
                    Owner = Window.GetWindow(this),
                    Icon = new WriteableBitmap(element.Icon as BitmapSource)
                };
                window.ShowDialog();
            }
        }

    }
}
