using HACGUI.Extensions;
using HACGUI.Services;
using HACGUI.Utilities;
using LibHac;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
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

                SendLockpickButton.IsEnabled = InjectService.LibusbKInstalled;
                MountSDButton.IsEnabled = InjectService.LibusbKInstalled;

                InjectService.DeviceInserted += () =>
                {
                    if(InjectService.LibusbKInstalled)
                        Dispatcher.Invoke(() => 
                        {
                            SendLockpickButton.IsEnabled = true;
                            MountSDButton.IsEnabled = true;
                        });
                };

                InjectService.DeviceRemoved += () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        SendLockpickButton.IsEnabled = false;
                        MountSDButton.IsEnabled = false;
                    });
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

        private void SendLockpickButtonClicked(object sender, RoutedEventArgs e)
        {
            GitHubClient gitClient = new GitHubClient(new ProductHeaderValue("Github"));
            if (!HACGUIKeyset.TempLockpickPayloadFileInfo.Exists)
                gitClient.Repository.Release.GetAll("shchmue", "Lockpick_RCM")
                    .ContinueWith(task =>
                    {
                        IReadOnlyList<Release> releases = task.Result;
                        Release release = releases.FirstOrDefault();
                        return release.Assets.FirstOrDefault(r => r.BrowserDownloadUrl.EndsWith(".bin"));
                    }).ContinueWith(task =>
                    {
                        ReleaseAsset asset = task.Result;
                        HttpClient httpClient = new HttpClient();
                        httpClient.GetStreamAsync(asset.BrowserDownloadUrl).ContinueWith(streamTask =>
                        {
                            Stream src = streamTask.Result;
                            using (Stream dest = HACGUIKeyset.TempLockpickPayloadFileInfo.OpenWrite())
                                src.CopyTo(dest);
                        }).Wait();
                    }).Wait();
            InjectService.SendPayload(HACGUIKeyset.TempLockpickPayloadFileInfo);
        }

        private void MountSDButtonClicked(object sender, RoutedEventArgs e)
        {
            InjectService.SendPayload(HACGUIKeyset.MemloaderPayloadFileInfo);
            InjectService.SendIni(HACGUIKeyset.MemloaderSampleFolderInfo.GetFile("ums_sd.ini"));
        }
    }
}
