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
    /// Interaction logic for ApplicationSaveTab.xaml
    /// </summary>
    public partial class ApplicationSaveTab : UserControl
    {
        private ApplicationElement Element => ApplicationWindow.Current.Element;

        public ApplicationSaveTab()
        {
            InitializeComponent();
            Content = new SaveManager.SaveManagerPage(Element.BaseTitleId);
        }
    }
}
