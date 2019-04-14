using HACGUI.Services;
using LibHac.IO;
using LibHac.Nand;
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
            FatFileSystemProvider partition = null;
            string partitionName = "";
            switch (ComboBox.SelectedValue)
            {
                case NANDMountType.PRODINFOF:
                    partition = NANDService.NAND.OpenProdInfoF();
                    partitionName = "PRODINFOF";
                    break;
                case NANDMountType.SAFE:
                    partition = NANDService.NAND.OpenSafePartition();
                    partitionName = "SAFE";
                    break;
                case NANDMountType.SYSTEM:
                    partition = NANDService.NAND.OpenSystemPartition();
                    partitionName = "SYSTEM";
                    break;
                case NANDMountType.USER:
                    partition = NANDService.NAND.OpenUserPartition();
                    partitionName = "USER";
                    break;
                default:
                    return;
            }

            MountService.Mount(new MountableFileSystem(partition, $"NAND ({partitionName})", "FAT32", OpenMode.Read));
        }
    }
}
