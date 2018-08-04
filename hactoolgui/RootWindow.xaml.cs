using HACGUI.FirstStart;
using HACGUI.Main;
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

namespace HACGUI
{
    /// <summary>
    /// Interaction logic for RootWindow.xaml
    /// </summary>
    public partial class RootWindow : NavigationWindow
    {
        public RootWindow()
        {
            InitializeComponent();

            ShowsNavigationUI = false;

            this.Loaded += (a, b) =>
            {
                var settings = HACGUI.Properties.Settings.Default;
                String path = settings.InstallPath;
                if (!String.IsNullOrEmpty(path))
                    Navigate(new MainWindow());
                else
                    Navigate(new Intro());
            };
        }
    }
}
