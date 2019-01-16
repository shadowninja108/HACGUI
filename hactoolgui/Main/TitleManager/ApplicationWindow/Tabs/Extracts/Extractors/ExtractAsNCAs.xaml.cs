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
using HACGUI.Main.Tasks;

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
                        if (File.Exists(ticketFileName))
                        {
                            LocalFile sourceTikFile = new LocalFile(root.GetFile(ticketFileName).FullName, OpenMode.Read);
                            if (!foundTickets.Contains(rightsId))
                            {
                                FileInfo destinationFileInfo = root.GetFile(ticketFileName);
                                foundTickets.Add(rightsId);
                                destinationFileInfo.Create().Close();
                                LocalFile destinationTikFile = new LocalFile(destinationFileInfo.FullName, OpenMode.Append & OpenMode.Write);
                                tasks.Add(new CopyTask(new FileStorage(sourceTikFile), new FileStorage(destinationTikFile), $"Copying {ticketFileName}..."));
                            }
                        }
                    }
                }
            }

            foreach (Nca nca in SelectedNcas)
            {
                FileInfo destinationNcaFileInfo = root.GetFile(nca.Filename);
                destinationNcaFileInfo.Create().Close();
                LocalFile destinationNcaFile = new LocalFile(destinationNcaFileInfo.FullName, OpenMode.Append & OpenMode.Write);
                IStorage source = nca.GetStorage();
                tasks.Add(new CopyTask(source, new FileStorage(destinationNcaFile), $"Copying {nca.Filename}..."));
            }
            ProgressView view = new ProgressView(tasks);
            NavigationWindow window = new NavigationWindow
            {
                ShowsNavigationUI = false // get rid of the t r a s h
            };
            window.Navigate(view);

            Task[] final = new Task[tasks.Count];
            for (int i = 0; i < final.Length; i++)
                final[i] = tasks[i].StartAsync();            

            RootWindow.Current.Submit(new Task(() =>
            {
                foreach (Task task in final)
                    task.RunSynchronously();
            }));

            window.ShowDialog();
        }
    }
}
