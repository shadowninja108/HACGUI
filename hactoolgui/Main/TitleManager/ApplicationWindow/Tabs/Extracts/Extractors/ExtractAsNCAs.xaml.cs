using HACGUI.Extensions;
using HACGUI.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.WindowsAPICodePack.Dialogs;
using LibHac.IO;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TaskManager;
using LibHac.IO.NcaUtils;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors
{
    /// <summary>
    /// Interaction logic for ExtractAsNCAs.xaml
    /// </summary>
    public partial class ExtractAsNCAs : IExtractorWindow
    {
        public ExtractAsNCAs(List<Nca> selected) : base(selected)
        {
            InitializeComponent();
        }

        private void BrowseClicked(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Path.Text = dlg.FileName;
            }
        }

        private void ExtractClicked(object sender, RoutedEventArgs e)
        {
            DirectoryInfo root = new DirectoryInfo(Path.Text);
            root.Create(); // ensure that the folder exists
            List<ProgressTask> tasks = new List<ProgressTask>();
            if (TicketCheckbox.IsChecked == true)
            {
                DirectoryInfo ticketDir = HACGUIKeyset.GetTicketsDirectory(Preferences.Current.DefaultConsoleName); // TODO: load console name from continuous location
                List<string> foundTickets = new List<string>();
                foreach (Nca nca in SelectedNcas)
                {
                    if (nca.HasRightsId)
                    {
                        string rightsId = BitConverter.ToString(nca.Header.RightsId).Replace("-", "").ToLower();
                        string ticketFileName = rightsId + ".tik";
                        FileInfo sourceTikFileInfo = ticketDir.GetFile(ticketFileName);
                        if (sourceTikFileInfo.Exists)
                        {
                            FileInfo destinationTikFileInfo = root.GetFile(ticketFileName);
                            if (!foundTickets.Contains(rightsId))
                            {
                                foundTickets.Add(rightsId);
                                destinationTikFileInfo.CreateAndClose();
                                LocalFile sourceTikFile = new LocalFile(sourceTikFileInfo.FullName, OpenMode.Read);
                                LocalFile destinationTikFile = new LocalFile(destinationTikFileInfo.FullName, OpenMode.Write);
                                destinationTikFile.SetSize(sourceTikFile.GetSize());
                                tasks.Add(new CopyTask($"Copying {ticketFileName}...", new FileStorage(sourceTikFile), new FileStorage(destinationTikFile)));
                            }
                        }
                    }
                }
            }

            foreach (Nca nca in SelectedNcas)
            {
                FileInfo destinationNcaFileInfo = root.GetFile(nca.Filename);
                destinationNcaFileInfo.CreateAndClose();
                LocalFile destinationNcaFile = new LocalFile(destinationNcaFileInfo.FullName, OpenMode.Write);
                IStorage source = nca.GetStorage();
                tasks.Add(new RunTask($"Allocating space for {nca.Filename}...", new Task(() => 
                {
                    destinationNcaFile.SetSize(source.GetSize());
                })));
                tasks.Add(new CopyTask($"Copying {nca.Filename}...", source, new FileStorage(destinationNcaFile)));
            }
            ProgressView view = new ProgressView(tasks);
            NavigationWindow window = new NavigationWindow
            {
                ShowsNavigationUI = false // get rid of the t r a s h
            };
            window.Navigate(view);

            TaskManagerPage.Current.Queue.Submit(tasks);

            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
    }
}
