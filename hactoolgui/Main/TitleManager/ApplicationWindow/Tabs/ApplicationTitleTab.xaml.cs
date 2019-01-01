using HACGUI.Main.TitleManager.Application.Tabs.Extracts.Extractors;
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

        public class TitleInfo
        {
            public Title Title;
            public TitleType Type => Title.Metadata.Type;
            public ulong TitleId => Title.Id;
            public long Size => Title.GetSize();
            public bool Selected { get; set; }
        }

        public ApplicationTitleTab()
        {
            InitializeComponent();

            foreach(Title title in Element.OrderTitlesByBest())
            {
                TitleInfo info = new TitleInfo
                {
                    Title = title
                };
                ListView.Items.Add(info);
            }
        }

        private void ExtractClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            List<Title> selected = new List<Title>();
            foreach(TitleInfo info in ListView.Items)
                if (info.Selected)
                    selected.Add(info.Title);

            Window window = new ExtractPickerWindow(selected);
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
    }
}
