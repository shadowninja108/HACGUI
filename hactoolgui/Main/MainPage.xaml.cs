using HACGUI.Extensions;
using HACGUI.Main.SaveManager;
using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TitleManager;
using HACGUI.Services;
using HACGUI.Utilities;
using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using LibHac.Spl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static HACGUI.Extensions.Extensions;

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

                SDService.OnSDPluggedIn += (effectiveRoot) =>
                {
                    DirectoryInfo root = SDService.SDRoot;
                    DirectoryInfo switchDir = root.GetDirectory("switch");
                    if (switchDir.Exists)
                    {
                        FileInfo prodKeysInfo = switchDir.GetFile("prod.keys");
                        if (prodKeysInfo.Exists)
                        {
                            try
                            {
                                ExternalKeyReader.ReadKeyFile(HACGUIKeyset.Keyset, prodKeysInfo.FullName);
                                new SaveKeysetTask(null).CreateTask().RunSynchronously();
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show($"An error occured when attempting to import keys from SD card. It could be corrupted.\nError: {e.Message}");
                            }
                        }
                    }
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
                    storages.Add(file.Open(FileMode.Open).AsStorage().AsReadOnly()); // Change to Open when write support is added
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
            Process.Start("explorer.exe", $"/open, {HACGUIKeyset.UserSwitchDirectoryInfo.FullName}");
        }

        private void DumpNANDToFileClicked(object sender, RoutedEventArgs e)
        {
            FileInfo info = RequestSaveFileFromUser("rawnand.bin", "NAND dump(.bin) | rawnand.bin");
            if (info != null) {
                IStorage source = NANDService.NANDSource;
                info.CreateAndClose();
                LocalFile target = new LocalFile(info.FullName, OpenMode.ReadWrite);
                IStorage targetStorage = target.AsStorage();
                source.GetSize(out long size);
                TaskManagerPage.Current.Queue.Submit(new ResizeTask($"Allocating space for {info.Name} (NAND backup)...", target, size));
                TaskManagerPage.Current.Queue.Submit(new CopyTask($"Copying NAND to {info.Name}...", source, targetStorage));
                MessageBox.Show("This is a lengthy operation.\nYou can check the status of it under\nthe tasks tab.");
            }
        }

        private static void TryImportTicket(FileInfo file)
        {

        }

        private void ImportGameDataClicked(object sender, RoutedEventArgs arg)
        {
            string filter = @"
                Any game data (*.xci,*.nca,*.nsp,*.tik)|*.xci;*.nca;*.nsp;*.tik|
                NX Card Image (*.xci)|*.xci|
                Nintendo Content Archive (*.nca)|*.nca|
                Nintendo Installable Package (*.nsp)|*.nsp|
                Ticket (*.tik)|*.tik
            ".FilterMultilineString();
            FileInfo[] files = RequestOpenFilesFromUser(".*", filter, "Select game data...");

            if (files == null)
                return;

            TaskManagerPage.Current.Queue.Submit(new RunTask("Processing imported game data...", new Task(() =>
            {
                IEnumerable<FileInfo> tickets = files.Where((f) =>
                {
                    try
                    {
                        using Stream s = f.OpenRead();
                        Ticket t = new Ticket(new BinaryReader(s));
                        t.GetTitleKey(HACGUIKeyset.Keyset);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
                IEnumerable<FileInfo> ncas = files.Except(tickets).Where((f) =>
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

                bool foundTicket = false;
                foreach (FileInfo tik in tickets)
                {
                    using Stream s = tik.OpenRead();
                    Ticket t = new Ticket(new BinaryReader(s));
                    HACGUIKeyset.Keyset.ExternalKeySet.Add(new RightsId(t.RightsId), new AccessKey(t.GetTitleKey(HACGUIKeyset.Keyset)));
                    foundTicket = true;
                }

                List<SwitchFs> switchFilesystems = new List<SwitchFs>();
                if (ncas.Any())
                    switchFilesystems.Add(SwitchFs.OpenNcaDirectory(HACGUIKeyset.Keyset, ncas.MakeFs()));

                foreach (FileInfo file in xcis)
                {
                    Xci xci = new Xci(HACGUIKeyset.Keyset, new LocalFile(file.FullName, OpenMode.Read).AsStorage());
                    switchFilesystems.Add(SwitchFs.OpenNcaDirectory(HACGUIKeyset.Keyset, xci.OpenPartition(XciPartitionType.Secure)));
                }

                foreach (FileInfo file in nsp)
                {
                    PartitionFileSystem fs = new PartitionFileSystem(new LocalFile(file.FullName, OpenMode.Read).AsStorage());
                    foreach(DirectoryEntryEx d in fs.EnumerateEntries().Where(e => e.Type == DirectoryEntryType.File && e.Name.EndsWith(".tik")))
                    {
                        fs.OpenFile(out IFile tikFile, d.FullPath.ToU8Span(), OpenMode.Read);
                        using (tikFile) {
                            Ticket t = new Ticket(new BinaryReader(tikFile.AsStream()));
                            try
                            {
                                HACGUIKeyset.Keyset.ExternalKeySet.Add(new RightsId(t.RightsId), new AccessKey(t.GetTitleKey(HACGUIKeyset.Keyset)));
                                foundTicket = true;
                            } catch(Exception)
                            {
                                MessageBox.Show("Failed to import .tik file included in NSP.");
                            }
                        }
                    }
                    switchFilesystems.Add(SwitchFs.OpenNcaDirectory(HACGUIKeyset.Keyset, fs));
                }

                if (foundTicket)
                {
                    TaskManagerPage.Current.Queue.Submit(new SaveKeysetTask(Preferences.Current.DefaultConsoleName));
                    MessageBox.Show("Ticket import done.");
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
