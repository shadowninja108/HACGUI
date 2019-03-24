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
using HACGUI.Services;
using HACGUI.Utilities;
using LibHac.IO;
using LibHac.IO.Save;
using System.Linq;

namespace HACGUI.Main.SaveManager
{
    /// <summary>
    /// Interaction logic for SaveInfoWindow.xaml
    /// </summary>
    public partial class SaveInfoWindow : Window
    {
        private SaveElement Element;

        public string SaveName => Element.DisplayName;
        public string SaveOwner => Element.Owner;
        public string Timestamp => CreateTimestamp();
        public ulong SaveID => Element.SaveId;

        public SaveInfoWindow(SaveElement element)
        {
            Element = element;
            InitializeComponent();

            if (Element.Owner == "Unknown")
            {
                foreach (var frame in new List<LabelBoxFrame>(FramePanel.Children.OfType<LabelBoxFrame>())
                    .Where(c => c.Label == "Owner"))
                    FramePanel.Children.Remove(frame);
            }

            if (string.Format("{0:x16}", SaveID) == SaveName)
            {
                foreach (var frame in new List<LabelBoxFrame>(FramePanel.Children.OfType<LabelBoxFrame>())
                    .Where(c => c.Label == "Name"))
                    FramePanel.Children.Remove(frame);
            }

            MountTypesComboBox.Items.Add(OpenType.ReadOnly);
            MountTypesComboBox.SelectedIndex = 0;
            if (IsWritable(element.Save))
                MountTypesComboBox.Items.Add(OpenType.Writable);

        }

        public string CreateTimestamp()
        {
            long timestamp = Element.Save.Header.ExtraData.Timestamp;
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            origin = origin.AddSeconds(timestamp);
            return $"{origin.ToShortTimeString()} | {origin.ToShortDateString()}";
        }

        private void MountClicked(object sender, RoutedEventArgs e)
        {
            ulong view = Element.SaveId;
            if (view == 0)
                view = Element.SaveId;
            OpenType type = (OpenType)MountTypesComboBox.SelectedItem;
            OpenMode mode = OpenMode.Read;
            switch (type)
            {
                case OpenType.Writable:
                    mode = OpenMode.ReadWrite;
                    break;
            }

            MountService.Mount(new MountableFileSystem(Element.Save, view.ToString("x16"), "Savefile", mode));
        }

        private static bool IsWritable(SaveDataFileSystem save)
        {
            // ok so FUCK nintendo
            // their filesystem interface doesn't define RW privileges
            // so I have to guess lol
            try
            {
                save.CreateDirectory("/tmp");
                save.RenameDirectory("/tmp", "/temp");
                save.DeleteDirectory("/temp");
                save.CreateFile("/tmp.bin", 0, CreateFileOptions.None);
                IFile temp = save.OpenFile("/tmp.bin", OpenMode.Write);
                byte[] testBytes = new byte[] { 0xBA, 0xDF, 0x00, 0xD };
                temp.Write(testBytes, 0);
                byte[] buff = new byte[4];
                temp.Read(buff, 0);
                save.RenameFile("/tmp.bin", "/temp.bin");
                save.DeleteFile("/temp.bin");
                if (!buff.SequenceEqual(testBytes))
                    return false;
                return true;
            } catch(NotImplementedException)
            {
                return false;
            }
        }

        public enum OpenType
        {
            ReadOnly, Writable
        }
    }
}
