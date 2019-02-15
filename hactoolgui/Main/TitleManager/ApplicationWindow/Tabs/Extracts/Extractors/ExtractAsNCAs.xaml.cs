using HACGUI.Extensions;
using HACGUI.Utilities;
using LibHac;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using LibHac.IO;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TaskManager;

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
                                tasks.Add(new CopyTask(new FileStorage(sourceTikFile), new FileStorage(destinationTikFile), $"Copying {ticketFileName}..."));
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
                    destinationNcaFile.SetSize(source.Length);
                })));
                tasks.Add(new CopyTask(source, new FileStorage(destinationNcaFile), $"Copying {nca.Filename}..."));
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
