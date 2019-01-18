using HACGUI.Main.TitleManager.Application.Tabs.Extracts.Extractors;
using HACGUI.Main.TitleManager.ApplicationWindow.Tabs;
using LibHac;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;

namespace HACGUI.Main.TitleManager.Application.Tabs
{
    /// <summary>
    /// Interaction logic for ApplicationTitleTab.xaml
    /// </summary>
    public partial class ApplicationTitleTab : UserControl
    {
        private ApplicationElement Element => ApplicationWindow.Current.Element;
        private Dictionary<ulong, LibHac.Application> Titles => ApplicationWindow.Current.Applications;

        public ApplicationTitleTab()
        {
            InitializeComponent();

            foreach(Title title in Element.OrderTitlesByBest())
            {
                TitleElement info = new TitleElement
                {
                    Title = title
                };
                ListView.Items.Add(info);
            }
        }

        private void ExtractClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            List<Nca> selected = new List<Nca>();
            foreach (TitleElement info in ListView.Items)
                if (info.Selected)
                    foreach (NcaElement nca in info.Ncas)
                        selected.Add(nca.Nca);

            Window window = new ExtractPickerWindow(selected);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();

            /*List<Nca> updates = new List<Nca>();
            List<Nca> normal = new List<Nca>();
            Title main = selected.FirstOrDefault(x => x.Metadata.Type == TitleType.Application);
            foreach (Title title in selected) {
                if (title.Metadata.Type == TitleType.Patch)
                    updates.Add(title.MainNca);
                else if(title != main)
                    normal.Add(title.MainNca);
            }

            if(updates.Any() && main == null)
            {
                // no base game found
                ;
            }

            Dictionary<SectionType, List<NcaSection>> indexed = new Dictionary<SectionType, List<NcaSection>>();
            foreach (Title title in selected)
            {
                Nca nca = title.MainNca;
                if (nca.Header.ContentType != ContentType.Meta)
                    foreach (NcaSection section in nca.Sections)
                    {
                        if (section == null) continue;
                        if (!indexed.ContainsKey(section.Type)) indexed[section.Type] = new List<NcaSection>();
                        indexed[section.Type].Add(section);
                    }
                //string path = Path.Combine(folder, nca.Filename);
                //nca.GetStream().CopyTo(new FileStream(path, FileMode.Create));

            }
            ;*/
        }

        private void TitleDoubleClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListView.SelectedItem is TitleElement title)
            {
                Window window = new TitleInfoWindow(title)
                {
                    Owner = Window.GetWindow(this),
                    Title = $"Titles for {Element.Name}"
                };
                window.ShowDialog();
            }
        }
    }
}
