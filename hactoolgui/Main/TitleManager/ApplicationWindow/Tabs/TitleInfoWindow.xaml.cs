using HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors;
using LibHac;
using LibHac.FsSystem.NcaUtils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    /// <summary>
    /// Interaction logic for TitleInfoWindow.xaml
    /// </summary>
    public partial class TitleInfoWindow : Window
    {
        public TitleInfoWindow(TitleElement title)
        {
            InitializeComponent();

            foreach(NcaElement nca in title.Ncas)
                ListView.Items.Add(nca);
        }

        private void ExtractClicked(object sender, RoutedEventArgs e)
        {
            List<SwitchFsNca> selected = new List<SwitchFsNca>();
            foreach (NcaElement info in ListView.Items)
                if (info.Selected)
                    selected.Add(info.Nca);

            Window window = new ExtractPickerWindow(selected)
            {
                Owner = GetWindow(this)
            };
            window.ShowDialog();
        }

        private void CopyNameClicked(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            ContextMenu contextMenu = item.Parent as ContextMenu;
            ListView listView = contextMenu.PlacementTarget as ListView;
            if (listView.SelectedItem is NcaElement element)
                Clipboard.SetText(string.Format("{0:x16}", element.FileName));
        }

        private void MountClicked(object sender, RoutedEventArgs e)
        {
            List<SwitchFsNca> selected = new List<SwitchFsNca>();
            foreach (NcaElement info in ListView.Items)
                if (info.Selected)
                    selected.Add(info.Nca);


            Dictionary<NcaFormatType, List<Tuple<SwitchFsNca, int>>> indexed = new Dictionary<NcaFormatType, List<Tuple<SwitchFsNca, int>>>();
            foreach (SwitchFsNca nca in selected)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!nca.Nca.Header.IsSectionEnabled(i)) continue;
                    NcaFsHeader section = nca.Nca.Header.GetFsHeader(i);
                    if (!indexed.ContainsKey(section.FormatType)) indexed[section.FormatType] = new List<Tuple<SwitchFsNca, int>>();
                    indexed[section.FormatType].Add(new Tuple<SwitchFsNca, int>(nca, i));
                }
            }

            Window window = new TitleMountDialog(indexed)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
    }
}
