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

namespace HACGUI.FirstStart
{
    /// <summary>
    /// Interaction logic for Intro.xaml
    /// </summary>
    public partial class Intro : Page
    {
        public Intro()
        {
            InitializeComponent();
            MinWidth = (double)Resources["MinWidth"];
            MinHeight = (double)Resources["MinHeight"];
        }

        private void StartFromBackups(object sender, RoutedEventArgs e)
        {
            NavigationWindow root = Extensions.FindParent<NavigationWindow>(this);
            
            root.Navigate(new PickNANDPage());
        }

        private void StartFromExistingInstall(object sender, RoutedEventArgs e)
        {

        }
    }
}
