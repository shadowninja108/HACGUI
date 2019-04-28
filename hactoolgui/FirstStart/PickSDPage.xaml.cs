using HACGUI.Extensions;
using HACGUI.Services;
using HACGUI.Utilities;
using LibHac;
using LibHac.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using static HACGUI.Extensions.Extensions;

namespace HACGUI.FirstStart
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class PickSDPage : PageExtension
    {
        private FileInfo KeysetFile;

        public PickSDPage()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                SDService.Validator = IsSDCard;
                SDService.OnSDPluggedIn += (drive) =>
                {
                    KeysetFile = drive.RootDirectory.GetDirectory("switch").GetFile("prod.keys");
                    Dispatcher.BeginInvoke(new Action(() => // Update on the UI thread
                    {
                        NextButton.IsEnabled = true;
                    }));
                };
                SDService.OnSDRemoved += (drive) =>
                {
                    HACGUIKeyset.Keyset = new HACGUIKeyset();
                    HACGUIKeyset.Keyset.LoadCommon();
                    Dispatcher.BeginInvoke(new Action(() => // Update on the UI thread
                    {
                        NextButton.IsEnabled = false;
                    }));
                };

                RootWindow.Current.Submit(new System.Threading.Tasks.Task(() => SDService.Start()));
            };
        }

        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationWindow root = FindRoot();

            // Reset SDService so that it's ready for later
            SDService.ResetHandlers();
            SDService.Stop();

            root.Navigate(new DerivingPage((page) => 
            {
                // setup key derivation task and execute it asynchronously on the next page
                CopyKeyset();

                HACGUIKeyset.Keyset.DeriveKeys();

                PageExtension next = null;

                // move to next page (after the task is complete)
                page.Dispatcher.BeginInvoke(new Action(() => // move to UI thread
                {
                    next = new PickNANDPage();
                    page.FindRoot().Navigate(next);
                })).Wait(); // must wait, otherwise a race condition may occur

                return next; // return the page we are navigating to
            }));
        }

        public override void OnBack()
        {
            SDService.Stop();
        }

        private void ManualPickerButtonClick(object sender, RoutedEventArgs e)
        {
            FileInfo info = RequestOpenFileFromUser(".keys", "Production keys file (.keys)|*.keys", "Pick a prod.keys file (most recent as possible)...", "prod.keys");
            if(info != null && info.Exists)
            {
                KeysetFile = info;
                Dispatcher.Invoke(() => NextButton.IsEnabled = true);
            }
        }

        private void HyperlinkClicked(object sender, RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

        public void CopyKeyset()
        {
            ExternalKeys.ReadKeyFile(HACGUIKeyset.Keyset, KeysetFile.FullName);
            HACGUIKeyset.Keyset.DeriveKeys(); // derive from keyblobs
        }

        private bool IsSDCard(DirectoryInfo info)
        {
            DirectoryInfo switchFolder = info.GetDirectory("switch");
            return switchFolder.Exists ? switchFolder.GetFile("prod.keys").Exists : false;
        }
    }
}
