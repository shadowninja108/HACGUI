using System;
using System.Windows;
using System.Configuration;
using System.Windows.Controls;
using HACGUI.FirstStart;
using System.Windows.Navigation;

namespace HACGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static StartupEventArgs Args { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Args = e;
        }
    }
}
