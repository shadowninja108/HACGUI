using HACGUI.Extensions;
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
using LibHac;
using Application = LibHac.Application;

namespace HACGUI.Main.TitleManager.Application
{
    /// <summary>
    /// Interaction logic for ApplicationView.xaml
    /// </summary>
    public partial class ApplicationWindow : Window
    {
        public static ApplicationWindow Current;

        public ApplicationElement Element;
        public Dictionary<ulong, LibHac.Application> Applications;

        public ApplicationWindow(ApplicationElement element)
        {
            Current = this;
            Element = element;
            Title = element.Name ?? "Unknown Title";

            InitializeComponent();
        }
    }
}
