using HACGUI.Services;
using LibHac.IO;
using LibHac.Nand;
using System.Windows;
using System.Windows.Controls;

namespace HACGUI.Main.TitleManager
{
    /// <summary>
    /// Interaction logic for NANDMounterWindow.xaml
    /// </summary>
    public partial class NANDMounterWindow : Window
    {
        public NANDMountType SelectedType { get; set; }

        public NANDMounterWindow()
        {
            InitializeComponent();

            ComboBox.Items.Add(NANDMountType.PRODINFOF);
            ComboBox.Items.Add(NANDMountType.SAFE);
            ComboBox.Items.Add(NANDMountType.SYSTEM);
            ComboBox.Items.Add(NANDMountType.USER);
        }

        public enum NANDMountType
        {
            PRODINFOF, SAFE, SYSTEM, USER
        }

        private void MountClicked(object sender, RoutedEventArgs e)
        {
            FatFileSystemProvider partition;
            string partitionName;
            string formatName;
            switch (ComboBox.SelectedValue)
            {
                case NANDMountType.PRODINFOF:
                    partition = NANDService.NAND.OpenProdInfoF();
                    partitionName = "PRODINFOF";
                    formatName = "FAT12";
                    break;
                case NANDMountType.SAFE:
                    partition = NANDService.NAND.OpenSafePartition();
                    partitionName = "SAFE";
                    formatName = "FAT32";
                    break;
                case NANDMountType.SYSTEM:
                    partition = NANDService.NAND.OpenSystemPartition();
                    partitionName = "SYSTEM";
                    formatName = "FAT32";
                    break;
                case NANDMountType.USER:
                    partition = NANDService.NAND.OpenUserPartition();
                    partitionName = "USER";
                    formatName = "FAT32";
                    break;
                default:
                    return;
            }

            MountService.Mount(new MountableFileSystem(partition, $"NAND ({partitionName})", formatName, OpenMode.ReadWrite));
        }
    }
}
