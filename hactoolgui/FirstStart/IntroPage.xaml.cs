using HACGUI.Extensions;
using System;
using System.Windows;
using System.Windows.Navigation;

namespace HACGUI.FirstStart
{
    /// <summary>
    /// Interaction logic for Intro.xaml
    /// </summary>
    public partial class IntroPage : PageExtension
    {
        public IntroPage() : base()
        {
            InitializeComponent();
        }

        private void StartFromBackups(object sender, RoutedEventArgs e)
        {
            NavigationWindow root = FindRoot();
            root.Navigate(new PickConsolePage());
            Console.WriteLine(new HACGUIKeyset().IsValidInstall().Item2);
        }

        private void StartFromExistingInstall(object sender, RoutedEventArgs e)
        {

        }
    }
}
