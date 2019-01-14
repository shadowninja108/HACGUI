using HACGUI.Extensions;
using HACGUI.Main.Tasks;
using HACGUI.Utilities;
using LibHac;
using LibHac.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors
{
    /// <summary>
    /// Interaction logic for RepackToNSPWindow.xaml
    /// </summary>
    public partial class RepackAsNSPWindow : IExtractorWindow
    {
        public RepackAsNSPWindow(List<Nca> selected) : base(selected)
        {
            InitializeComponent();
        }

        private void BrowseClicked(object sender, RoutedEventArgs e)
        {
            FileInfo file = Extensions.Extensions.RequestSaveFileFromUser(".nsp", "Nintendo Switch Package (.nsp)|*.nsp");
            if (file != null)
                Path.Text = file.FullName;
        }

        private void RepackClicked(object sender, RoutedEventArgs e)
        {
            FileInfo info = new FileInfo(Path.Text);
            info.Directory.Create(); // ensure that the folder exists
            Pfs0Builder builder = new Pfs0Builder();
            ProgressTask logger = new NullTask();
            ProgressView view = new ProgressView(new List<ProgressTask> { logger });
            if (TicketCheckbox.IsChecked == true)
            {
                DirectoryInfo ticketDir = HACGUIKeyset.GetTicketsDirectory(Preferences.Current.DefaultConsoleName); // TODO: load console name from continuous location
                List<string> foundTickets = new List<string>();
                foreach (Nca nca in SelectedNcas)
                {
                    if (nca.HasRightsId)
                    {
                        string rightsId = BitConverter.ToString(nca.Header.RightsId).Replace("-", "").ToLower();
                        FileInfo ticketFile = ticketDir.GetFile(rightsId + ".tik");
                        if (ticketFile.Exists && !foundTickets.Contains(rightsId))
                        {
                            foundTickets.Add(rightsId);
                            builder.AddFile(ticketFile.Name, ticketFile.OpenRead());
                        }
                    }
                }
            }

            foreach (Nca nca in SelectedNcas)
                builder.AddFile(nca.Filename);

            NavigationWindow window = new NavigationWindow
            {
                ShowsNavigationUI = false // get rid of the t r a s h
            };
            window.Navigate(view);
            RootWindow.Current.Submit(new Task(() => builder.Build(info.Create(), logger)));
            window.ShowDialog();
        }
    }
}
