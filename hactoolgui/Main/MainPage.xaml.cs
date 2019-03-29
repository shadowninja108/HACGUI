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
using System.Linq;

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
                    Dispatcher.Invoke(() => 
                    {
                        foreach (MenuItem item in
                            NANDContextMenu.Items.Cast<MenuItem>().Where(i => i.Tag as string == "RequiresNAND"))
                            item.IsEnabled = true;
                    });

                };

                NANDService.OnNANDRemoved += () =>
                {
                    foreach (MenuItem item in
                        NANDContextMenu.Items.Cast<MenuItem>().Where(i => i.Tag as string == "RequiresNAND"))
                        item.IsEnabled = false;
                };

                // init this first as other pages may request tasks on init
                TaskManagerView = new TaskManagerPage();
                TaskManagerFrame.Content = TaskManagerView;

                TaskManagerView.Queue.Submit(new RunTask("Opening/Deriving keys...", new Task(() => HACGUIKeyset.Keyset.LoadAll())));

                DeviceService.Start();

                TitleManagerView = new MainTitleManagerPage();
                TitleManagerFrame.Content = TitleManagerView;
                SaveManagerView = new SaveManagerPage(0);
                SaveManagerFrame.Content = SaveManagerView;

                StatusService.Bar = StatusBar;

                StatusService.Start();

                if (IsAdministrator)
                    AdminButton.IsEnabled = false;
            };
        }

        private void PickNANDButtonClick(object sender, RoutedEventArgs e)
        {
            FileInfo[] files = RequestOpenFilesFromUser(".bin", "Raw NAND dump (.bin or .bin.*)|*.bin*", "Select raw NAND dump", "rawnand.bin");

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
            Window window = new NANDMounterWindow()
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
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
            FileInfo[] files = RequestOpenFilesFromUser(".*", filter, "Select the game data");

            ;
        }
    }
}
