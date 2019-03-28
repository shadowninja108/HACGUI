using HACGUI.Extensions;
using LibHac;
using System.Diagnostics;
using System.Windows;

namespace HACGUI.FirstStart
{
    /// <summary>
    /// Interaction logic for Finish.xaml
    /// </summary>
    public partial class FinishPage : PageExtension
    {
        public FinishPage()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                TextArea.Text += HACGUIKeyset.PrintCommonKeys(HACGUIKeyset.Keyset, true);
                TextArea.Text += "--------------------------------------------------------------\n";
                TextArea.Text += HACGUIKeyset.PrintCommonWithoutFriendlyKeys(HACGUIKeyset.Keyset);
                TextArea.Text += "--------------------------------------------------------------\n";
                TextArea.Text += ExternalKeys.PrintUniqueKeys(HACGUIKeyset.Keyset);
                TextArea.Text += "--------------------------------------------------------------\n";
                TextArea.Text += ExternalKeys.PrintTitleKeys(HACGUIKeyset.Keyset);
            };
        }

        private void OpenKeysClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(HACGUIKeyset.UserSwitchDirectoryInfo.FullName);
        }
    }
}
