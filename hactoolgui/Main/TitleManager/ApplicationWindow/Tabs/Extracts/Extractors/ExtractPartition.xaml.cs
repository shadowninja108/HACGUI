using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.WindowsAPICodePack.Dialogs;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TaskManager;
using LibHac;
using LibHac.Fs;
using LibHac.FsSystem.NcaUtils;
using LibHac.FsSystem;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors
{
    /// <summary>
    /// Interaction logic for ExtractAsNCAs.xaml
    /// </summary>
    public partial class ExtractPartition : IExtractorWindow
    {
        private Dictionary<NcaFormatType, List<Tuple<SwitchFsNca, int>>> Indexed;
        public ExtractPartition(List<SwitchFsNca> selected) : base(selected)
        {
            InitializeComponent();

            Indexed = new Dictionary<NcaFormatType, List<Tuple<SwitchFsNca, int>>>();
            foreach (SwitchFsNca nca in selected)
            {
                if (nca.Nca.Header.ContentType != NcaContentType.Meta)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (!nca.Nca.Header.IsSectionEnabled(i)) continue;
                        NcaFsHeader section = nca.Nca.Header.GetFsHeader(i);
                        if (!Indexed.ContainsKey(section.FormatType)) Indexed[section.FormatType] = new List<Tuple<SwitchFsNca, int>>();
                        Indexed[section.FormatType].Add(new Tuple<SwitchFsNca, int>(nca, i));
                    }
                }
            }

            if (Indexed.ContainsKey(NcaFormatType.Romfs))
                ComboBox.Items.Add(MountType.Romfs);
            if (Indexed.ContainsKey(NcaFormatType.Pfs0))
                ComboBox.Items.Add(MountType.Exefs);
        }

        private void BrowseClicked(object sender, RoutedEventArgs e)
        {
            using (CommonOpenFileDialog dlg = new CommonOpenFileDialog())
            {
                dlg.IsFolderPicker = true;
                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Path.Text = dlg.FileName;
                }
            }
        }

        private void ExtractClicked(object sender, RoutedEventArgs e)
        {
            MountType mountType = (MountType)ComboBox.SelectedItem;
            NcaFormatType sectionType = NcaFormatType.Romfs;
            switch (mountType)
            {
                case MountType.Exefs:
                    sectionType = NcaFormatType.Pfs0;
                    break;
                case MountType.Romfs:
                    sectionType = NcaFormatType.Romfs;
                    break;
            }

            IEnumerable<Tuple<SwitchFsNca, int>> list = Indexed[sectionType];
            string path = Path.Text;
            TaskManagerPage.Current.Queue.Submit(new RunTask("Opening filesystems to extract...", new Task(() =>
            {
                List<IFileSystem> filesystems = new List<IFileSystem>();
                foreach (Tuple<SwitchFsNca, int> t in list)
                {
                    SwitchFsNca nca = t.Item1;
                    NcaFsHeader section = t.Item1.Nca.Header.GetFsHeader(t.Item2);
                    int index = t.Item2;

                    filesystems.Add(nca.OpenFileSystem(index, IntegrityCheckLevel.ErrorOnInvalid));
                }
                filesystems.Reverse();

                LayeredFileSystem lfs = new LayeredFileSystem(filesystems);
                ExtractFileSystemTask task = new ExtractFileSystemTask($"Extracting {sectionType}...", lfs, path);

                Dispatcher.InvokeAsync(() => 
                {
                    ProgressView view = new ProgressView(new List<ProgressTask>() { task });
                    NavigationWindow window = new NavigationWindow
                    {
                        ShowsNavigationUI = false // get rid of the t r a s h
                    };
                    window.Navigate(view);

                    TaskManagerPage.Current.Queue.Submit(task);

                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                }); 
            })));
        }
        public enum MountType
        {
            Romfs, Exefs
        }
    }
}
