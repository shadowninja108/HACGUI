using HACGUI.Extensions;
using HACGUI.Utilities;
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
                TextArea.Text += ExternalKeyReader.PrintUniqueKeys(HACGUIKeyset.Keyset);
                TextArea.Text += "--------------------------------------------------------------\n";
                TextArea.Text += ExternalKeyReader.PrintTitleKeys(HACGUIKeyset.Keyset);
            };
            HACGUIKeyset.SetConsoleDirectoryAsIncomplete(PickConsolePage.ConsoleName, false);
        }

        private void OpenKeysClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(HACGUIKeyset.UserSwitchDirectoryInfo.FullName);
        }
    }
}
