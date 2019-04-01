using HACGUI.Services;
using LibHac.IO;
using LibHac.IO.NcaUtils;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    /// <summary>
    /// Interaction logic for TitleMountDialog.xaml
    /// </summary>
    public partial class TitleMountDialog : Window
    {
        private Dictionary<SectionType, List<Tuple<Nca, NcaSection>>> Indexed;
        private Nca MainNca;

        public TitleMountDialog(Dictionary<SectionType, List<Tuple<Nca, NcaSection>>> indexed, Nca mainNca)
        {
            InitializeComponent();
            Indexed = indexed;
            MainNca = mainNca;

            if (Indexed.ContainsKey(SectionType.Romfs) | Indexed.ContainsKey(SectionType.Bktr))
                ComboBox.Items.Add(MountType.Romfs);
            if (Indexed.ContainsKey(SectionType.Pfs0))
                ComboBox.Items.Add(MountType.Exefs);
        }


        private void MountClicked(object sender, RoutedEventArgs e)
        {
            MountType mountType = (MountType) ComboBox.SelectedItem;
            SectionType sectionType = SectionType.Romfs;
            switch (mountType)
            {
                case MountType.Exefs:
                    sectionType = SectionType.Pfs0;
                    break;
                case MountType.Romfs:
                    sectionType = SectionType.Romfs;
                    if (!Indexed.ContainsKey(sectionType))
                        sectionType = SectionType.Bktr;
                    break;
            }
            List<IFileSystem> filesystems = new List<IFileSystem>();
            IEnumerable<Tuple<Nca, NcaSection>> list = Indexed[sectionType];
            if (mountType == MountType.Romfs && Indexed.ContainsKey(SectionType.Bktr))
                list = list.Concat(Indexed[SectionType.Bktr]);
            foreach (Tuple<Nca, NcaSection> t in list)
            {
                Nca nca = t.Item1;
                NcaSection section = t.Item2;
                if (section.Header.Type == SectionType.Bktr)
                    nca.SetBaseNca(MainNca);
                filesystems.Add(nca.OpenFileSystem(section.SectionNum, IntegrityCheckLevel.ErrorOnInvalid));
            }
            filesystems.Reverse();
            LayeredFileSystem fs = new LayeredFileSystem(filesystems);
            string typeString = sectionType.ToString();
            if (sectionType == SectionType.Bktr)
                typeString = $"{mountType} ({typeString})";
            MountService.Mount(new MountableFileSystem(fs, $"Mounted {mountType.ToString().ToLower()}", typeString, OpenMode.Read));
        }

        public enum MountType
        {
            Romfs, Exefs
        }
    }
}
