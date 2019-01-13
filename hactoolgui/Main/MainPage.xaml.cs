using HACGUI.Extensions;
using HACGUI.Main.TitleManager;
using HACGUI.Services;
using LibHac;
using LibHac.IO;
using LibHac.Nand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
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

        public MainPage()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                NANDService.OnNANDPluggedIn += () => 
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        for (int i = 1; i < 5; i++)
                        {
                            (NANDContextMenu.Items[i] as MenuItem).IsEnabled = true;
                        }
                    }));

                };

                NANDService.OnNANDRemoved += () =>
                {
                    for (int i = 1; i < 5; i++)
                    {
                        (NANDContextMenu.Items[i] as MenuItem).IsEnabled = false;
                    }
                };

                TitleManagerView = new MainTitleManagerPage();
                TitleManagerFrame.Content = TitleManagerView;
                StatusService.Bar = StatusBar;
                StatusService.Start();

                if (IsAdministrator)
                    AdminButton.IsEnabled = false;
            };
        }

        private void PickNANDButtonClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "rawnand.bin",
                DefaultExt = ".bin",
                Filter = "Raw NAND dump (.bin or .bin.*)|*.bin*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            { // it's nullable so i HAVE to compare it to true
                string[] files = dlg.FileNames;
                if (files != null && files.Length > 0)
                {
                    IList<IStorage> streams = new List<IStorage>();
                    foreach (string file in files)
                        streams.Add(new FileInfo(file).OpenRead().AsStorage()); // Change to Open when write support is added
                    IStorage NANDSource = new ConcatenationStorage(streams, true);

                    if (!NANDService.InsertNAND(NANDSource, false))
                    {
                        MessageBox.Show("Invalid NAND dump!");
                    }

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
                NandPartition partition = null;
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

                MountService.Mount(new MountableFileSystem(partition, $"NAND ({partitionName})"));
            } else
                MessageBox.Show("Dokan driver not installed.\nInstall it to use this feature.");
        }

        private void OpenUserSwitchClick(object sender, RoutedEventArgs e)
        {
            Process.Start($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}.switch");
        }
    }
}
