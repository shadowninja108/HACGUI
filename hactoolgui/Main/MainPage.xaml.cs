using HACGUI.Extensions;
using HACGUI.Main.SaveManager;
using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TitleManager;
using HACGUI.Utilities;
using HACGUI.Services;
using static HACGUI.Extensions.Extensions;
using LibHac;
using LibHac.Fs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.ComponentModel;
using LibHac.Fs.NcaUtils;

namespace HACGUI.Main
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainPage : PageExtension
    {
        private static MainTitleManagerPage TitleManagerView;
        private static SaveManagerPage SaveManagerView;
        private static TaskManagerPage TaskManagerView;

        public MainPage()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                void rcmRefresh(bool b)
                {
                    foreach (MenuItem item in
                        RCMContextMenu.Items.Cast<MenuItem>().Where(i => i.Tag as string == "RequiresRCM"))
                        item.IsEnabled = b;
                };
                rcmRefresh(InjectService.LibusbKInstalled);

                void nandRefresh(bool b)
                {
                    foreach (MenuItem item in
                        NANDContextMenu.Items.Cast<MenuItem>().Where(i => i.Tag as string == "RequiresNAND"))
                        item.IsEnabled = b;
                };
                nandRefresh(false);

                InjectService.DeviceInserted += () =>
                {
                    if(InjectService.LibusbKInstalled)
                        Dispatcher.Invoke(() => rcmRefresh(true));
                };

                InjectService.DeviceRemoved += () =>
                {
                    Dispatcher.Invoke(() => rcmRefresh(false));

                };

                NANDService.OnNANDPluggedIn += () => 
                {
                    Dispatcher.Invoke(() => nandRefresh(true));
                };

                NANDService.OnNANDRemoved += () =>
                {
                    Dispatcher.Invoke(() => nandRefresh(false));
                };

                // init this first as other pages may request tasks on init
                TaskManagerView = new TaskManagerPage();
                TaskManagerFrame.Content = TaskManagerView;

                StatusService.Bar = StatusBar;
                StatusService.CurrentTaskBlock = CurrentTaskBlock;
                CurrentTaskBlock.Background = StatusBar.Background;
                TaskManagerView.Queue.Submit(new RunTask("Opening/Deriving keys...", new Task(() => HACGUIKeyset.Keyset.LoadAll())));

                DeviceService.Start();

                TitleManagerView = new MainTitleManagerPage();
                TitleManagerFrame.Content = TitleManagerView;
                SaveManagerView = new SaveManagerPage(0);
                SaveManagerFrame.Content = SaveManagerView;

                StatusService.Start();

                if (Native.IsAdministrator)
                    AdminButton.IsEnabled = false;
            };
        }

        private void PickNANDButtonClick(object sender, RoutedEventArgs e)
        {
            FileInfo[] files = RequestOpenFilesFromUser(".bin", "Raw NAND dump (.bin or .bin.*)|*.bin*", "Select raw NAND dump...", "rawnand.bin");

            if (files != null)
            {
                IList<IStorage> storages = new List<IStorage>();
                foreach (FileInfo file in files)
                    storages.Add(file.OpenRead().AsStorage()); // Change to Open when write support is added
                IStorage NANDSource = new ConcatenationStorage(storages, true);

                if (!NANDService.InsertNAND(NANDSource, false))
                {
                    MessageBox.Show("Invalid NAND dump!");
                }
            }
        }

        private void RestartAsAdminButtonClicked(object sender, RoutedEventArgs e)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = AppDomain.CurrentDomain.FriendlyName;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.StartInfo.Arguments = $"continue";
            try
            {
                proc.Start();
                System.Windows.Application.Current.Shutdown();
            }
            catch (Win32Exception)
            {
            }
        }

        private void MountPartition(object sender, RoutedEventArgs e)
        {
            Window window = new NANDMounterWindow()
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }

        private void OpenUserSwitchClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(HACGUIKeyset.UserSwitchDirectoryInfo.FullName);
        }

        private void DumpNANDToFileClicked(object sender, RoutedEventArgs e)
        {
            FileInfo info = RequestSaveFileFromUser("rawnand.bin", "NAND dump(.bin) | rawnand.bin");
            if (info != null) {
                IStorage source = NANDService.NANDSource;
                info.CreateAndClose();
                LocalFile target = new LocalFile(info.FullName, OpenMode.ReadWrite);
                IStorage targetStorage = target.AsStorage();
                TaskManagerPage.Current.Queue.Submit(new ResizeTask($"Allocating space for {info.Name} (NAND backup)...", target, source.GetSize()));
                TaskManagerPage.Current.Queue.Submit(new CopyTask($"Copying NAND to {info.Name}...", source, targetStorage));
                MessageBox.Show("This is a lengthy operation.\nYou can check the status of it under\nthe tasks tab.");
            }
        }

        private void ImportGameDataClicked(object sender, RoutedEventArgs e)
        {
            string filter = @"
                Any game data (*.xci,*.nca,*.nsp)|*.xci;*.nca;*.nsp|
                NX Card Image (*.xci)|*.xci|
                Nintendo Content Archive (*.nca)|*.nca|
                Nintendo Installable Package (*.nsp)|*.nsp
            ".FilterMultilineString();
            FileInfo[] files = RequestOpenFilesFromUser(".*", filter, "Select game data...");

            if (files == null)
                return;

            TaskManagerPage.Current.Queue.Submit(new RunTask("Process imported game data...", new Task(() =>
            {
                IEnumerable<FileInfo> ncas = files.Where((f) =>
                {
                    try
                    {
                        new Nca(HACGUIKeyset.Keyset, new LocalFile(f.FullName, OpenMode.Read).AsStorage());
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
                IEnumerable<FileInfo> xcis = files.Except(ncas).Where((f) =>
                {
                    try
                    {
                        new Xci(HACGUIKeyset.Keyset, new LocalFile(f.FullName, OpenMode.Read).AsStorage());
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
                IEnumerable<FileInfo> nsp = files.Except(ncas).Except(xcis).Where((f) =>
                {
                    try
                    {
                        new PartitionFileSystem(new LocalFile(f.FullName, OpenMode.Read).AsStorage());
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });

                List<SwitchFs> switchFilesystems = new List<SwitchFs>();

                PseudoFileSystem ncaFs = new PseudoFileSystem();
                foreach (FileInfo file in ncas)
                {
                    LocalFileSystem fs = new LocalFileSystem(file.Directory.FullName);

                    // clean up filename so it only ends with .nca, then map to actual name
                    string s = file.Name;
                    while (s.EndsWith(".nca"))
                        s = s.Substring(0, s.IndexOf(".nca"));
                    ncaFs.Add($"/{s}.nca", $"/{file.Name}", fs);
                }
                if (ncas.Any())
                    switchFilesystems.Add(SwitchFs.OpenNcaDirectory(HACGUIKeyset.Keyset, ncaFs));

                foreach (FileInfo file in xcis)
                {
                    Xci xci = new Xci(HACGUIKeyset.Keyset, new LocalFile(file.FullName, OpenMode.Read).AsStorage());
                    switchFilesystems.Add(SwitchFs.OpenNcaDirectory(HACGUIKeyset.Keyset, xci.OpenPartition(XciPartitionType.Secure)));
                }

                foreach (FileInfo file in nsp)
                {
                    PartitionFileSystem fs = new PartitionFileSystem(new LocalFile(file.FullName, OpenMode.Read).AsStorage());
                    switchFilesystems.Add(SwitchFs.OpenNcaDirectory(HACGUIKeyset.Keyset, fs));
                }

                foreach (SwitchFs fs in switchFilesystems)
                    DeviceService.FsView.LoadFileSystemAsync("Opening imported data...", () => fs, FSView.TitleSource.Imported, false);
            })));
            
        }

        private void InjectPayloadClicked(object sender, RoutedEventArgs e)
        {
            FileInfo file = RequestOpenFileFromUser(".bin", "RCM payloads (.bin)|*.bin", "Select a payload...");
            if (file != null)
                InjectService.SendPayload(file);
        }

        private void SendMemloaderIniClicked(object sender, RoutedEventArgs e)
        {
            FileInfo file = RequestOpenFileFromUser(".ini", "Memloader ini (.ini)|*.ini", "Select a ini...");
            if (file != null)
                InjectService.SendIni(file);
        }

        private void InjectMemloaderPayload(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string iniFile = item.Tag + ".ini";
            InjectService.SendPayload(HACGUIKeyset.MemloaderPayloadFileInfo);
            InjectService.SendIni(HACGUIKeyset.MemloaderSampleFolderInfo.GetFile(iniFile));
        }
    }
}
