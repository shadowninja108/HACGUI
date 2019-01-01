using HACGUI.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace HACGUI.FirstStart
{
    /// <summary>
    /// Interaction logic for PickConsole.xaml
    /// </summary>
    public partial class PickConsolePage : PageExtension
    {
        public static string ConsoleName;

        public PickConsolePage()
        {
            InitializeComponent();
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            string text = ConsoleNameBox.Text;
            if (NextButton != null)
            {
                char[] invalidChars = Path.GetInvalidFileNameChars().Intersect(text).ToArray();
                NextButton.IsEnabled = invalidChars.Length == 0;
                if (!NextButton.IsEnabled)
                {
                    string tooltipText = $"Your console's name cannot contain the characters: {string.Join(",", invalidChars)}";
                    ToolTip tooltip = new ToolTip()
                    {
                        Content = tooltipText,
                    };
                    NextButton.ToolTip = tooltip;
                }
                else
                    NextButton.ToolTip = null;
            }
        }

        private void NextClick(object sender, System.Windows.RoutedEventArgs e)
        {
            ConsoleName = ConsoleNameBox.Text;
            HACGUIKeyset.RootFolderInfo.Create();
            HACGUIKeyset.RootConsoleFolderInfo.Create();

            // Navigate to next page
            NavigationWindow root = FindRoot();
            root.Navigate(new PickSDPage());
        }

        private void EnterKeyPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Enter)
                NextClick(sender, null);
        }
    }
}
