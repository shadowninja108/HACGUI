using System.Collections.Generic;
using System.Windows;
using Application = LibHac.Application;

namespace HACGUI.Main.TitleManager.ApplicationWindow
{
    /// <summary>
    /// Interaction logic for ApplicationView.xaml
    /// </summary>
    public partial class ApplicationWindow : Window
    {
        public static ApplicationWindow Current;

        public ApplicationElement Element;
        public Dictionary<ulong, Application> Applications;

        public ApplicationWindow(ApplicationElement element)
        {
            Current = this;
            Element = element;
            Title = element.Name ?? "Unknown Title";

            InitializeComponent();
        }
    }
}
