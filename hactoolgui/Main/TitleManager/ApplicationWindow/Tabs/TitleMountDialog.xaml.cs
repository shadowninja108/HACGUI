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

        public TitleMountDialog(Dictionary<SectionType, List<Tuple<Nca, NcaSection>>> indexed)
        {
            InitializeComponent();
            Indexed = indexed;

            if (Indexed.ContainsKey(SectionType.Romfs))
                ComboBox.Items.Add(MountType.Romfs);
            if (Indexed.ContainsKey(SectionType.Pfs0))
                ComboBox.Items.Add(MountType.Exefs);
        }


        private void MountClicked(object sender, RoutedEventArgs e)
        {
            MountType type = (MountType) ComboBox.SelectedItem;
            SectionType sectionType = SectionType.Romfs;
            switch (type)
            {
                case MountType.Exefs:
                    sectionType = SectionType.Pfs0;
                    break;
                case MountType.Romfs:
                    sectionType = SectionType.Romfs;
                    break;
            }
            List<IFileSystem> filesystems = new List<IFileSystem>();
            foreach (Tuple<Nca, NcaSection> t in Indexed[sectionType])
                filesystems.Add(t.Item1.OpenFileSystem(t.Item2.SectionNum, IntegrityCheckLevel.ErrorOnInvalid));
            LayeredFileSystem fs = new LayeredFileSystem(filesystems);
            MountService.Mount(new MountableFileSystem(fs, $"Mounted {sectionType.ToString().ToLower()}", sectionType.ToString(), OpenMode.Read));
        }

        public enum MountType
        {
            Romfs, Exefs
        }
    }
}
