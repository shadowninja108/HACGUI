using HACGUI.Extensions;
using HACGUI.Main.SaveManager;
using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TitleManager;
using HACGUI.Services;
using static HACGUI.Extensions.Extensions;
using LibHac;
using LibHac.IO;
using LibHac.Nand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
                NANDService.OnNANDPluggedIn += () => 
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        for (int i = 1; i < 6; i++)
                        {
                            (NANDContextMenu.Items[i] as MenuItem).IsEnabled = true;
                        }
                    }));

                };

                NANDService.OnNANDRemoved += () =>
                {
                    for (int i = 1; i < 6; i++)
                    {
                        (NANDContextMenu.Items[i] as MenuItem).IsEnabled = false;
                    }
                };

                // init this first as other pages may request tasks on init
                TaskManagerView = new TaskManagerPage();
                TaskManagerFrame.Content = TaskManagerView;

                TaskManagerView.Queue.Submit(new RunTask("Deriving keys...", new Task(() => HACGUIKeyset.Keyset.Load())));

                DeviceService.Start();

                TitleManagerView = new MainTitleManagerPage();
                TitleManagerFrame.Content = TitleManagerView;
                SaveManagerView = new SaveManagerPage();
                SaveManagerFrame.Content = SaveManagerView;

                StatusService.Bar = StatusBar;

                StatusService.Start();

                if (IsAdministrator)
                    AdminButton.IsEnabled = false;
            };
        }

        private void PickNANDButtonClick(object sender, RoutedEventArgs e)
        {
            FileInfo[] files = Extensions.Extensions.RequestOpenFilesFromUser(".bin", "Raw NAND dump (.bin or .bin.*)|*.bin*", "Select raw NAND dump", "rawnand.bin");

            if (files != null)
            {
                IList<IStorage> streams = new List<IStorage>();
                foreach (FileInfo file in files)
                    streams.Add(file.OpenRead().AsStorage()); // Change to Open when write support is added
                IStorage NANDSource = new ConcatenationStorage(streams, true);

                if (!NANDService.InsertNAND(NANDSource, false))
                {
                    MessageBox.Show("Invalid NAND dump!");
                }
            }
        }

        private void RestartAsAdminButtonClick(object sender, RoutedEventArgs e)
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
            catch (System.ComponentModel.Win32Exception)
            {
            }
        }

        public static bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);

        private void MountPartition(object sender, RoutedEventArgs e)
        {
            if (MountService.CanMount()) { 
                MenuItem button = sender as MenuItem;
                int partitionIndex = int.Parse((string)button.Tag);
                FatFileSystemProvider partition = null;
                string partitionName = "";
                switch (partitionIndex)
                {
                    case 0:
                        partition = NANDService.NAND.OpenProdInfoF();
                        partitionName = "PRODINFOF";
                        break;
                    case 1:
                        partition = NANDService.NAND.OpenSafePartition();
                        partitionName = "SAFE";
                        break;
                    case 2:
                        partition = NANDService.NAND.OpenSystemPartition();
                        partitionName = "SYSTEM";
                        break;
                    case 3:
                        partition = NANDService.NAND.OpenUserPartition();
                        partitionName = "USER";
                        break;
                    default:
                        return;
                }

                MountService.Mount(new MountableFileSystem(partition, $"NAND ({partitionName})", "FAT32", OpenMode.Read));
            } else
                MessageBox.Show("Dokan driver not installed.\nInstall it to use this feature.");
        }

        private void OpenUserSwitchClick(object sender, RoutedEventArgs e)
        {
            Process.Start(HACGUIKeyset.UserSwitchDirectoryInfo.FullName);
        }

        private void DumpNANDToFile(object sender, RoutedEventArgs e)
        {
            FileInfo info = RequestSaveFileFromUser("rawnand.bin", "NAND dump(.bin) | rawnand.bin");
            if (info != null) {
                IStorage source = NANDService.NANDSource;
                info.CreateAndClose();
                LocalFile target = new LocalFile(info.FullName, OpenMode.ReadWrite);
                IStorage targetStorage = target.AsStorage();
                TaskManagerPage.Current.Queue.Submit(new ResizeTask($"Allocating space for {info.Name} (NAND backup)...", target, source.Length));
                TaskManagerPage.Current.Queue.Submit(new CopyTask($"Copying NAND to {info.Name}...", source, targetStorage));
                MessageBox.Show("This is a lengthy operation.\nYou can check the status of it under\nthe tasks tab.");
            }
        }
    }
}
