using HACGUI.Extensions;
using HACGUI.Utilities;
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

            MissingLabel.Content = HACGUIKeyset.IsValidInstall().Item2;
        }

        private void StartButtonPressed(object sender, RoutedEventArgs e)
        {
            NavigationWindow root = FindNavigationWindow();
            root.Navigate(new PickConsolePage());
        }
    }
}
