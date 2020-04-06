using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HACGUI.Extensions;
using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TitleManager;
using HACGUI.Services;
using HACGUI.Utilities;
using LibHac.Common;
using LibHac.Fs;

namespace HACGUI.Main.SaveManager
{
    /// <summary>
    /// Interaction logic for SaveInfoWindow.xaml
    /// </summary>
    public partial class SaveInfoWindow : Window
    {
        private readonly SaveElement Element;

        public string SaveName => Element.DisplayName;
        public string SaveOwner => Element.Owner;
        public string Timestamp => CreateTimestamp();
        public ulong SaveID => Element.SaveId;
        public Guid SaveUserId => Element.UserId;
        public string UserString => Element.UserString;

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
            //if (IsWritable(element.Save))
            //    MountTypesComboBox.Items.Add(OpenType.Writable);

            Task<ImageSource> task = GetIconAync();
            ProfileIcon.Source = ApplicationElement.UnknownIcon;
            task.ContinueWith((source) => ProfileIcon.Dispatcher.Invoke(() => ProfileIcon.Source = source.Result));
            TaskManagerPage.Current.Queue.Submit(new RunTask($"Decoding profile icon...", task));
        }

        public Task<ImageSource> GetIconAync()
        {
            return new Task<ImageSource>(() =>
            {
                try
                {
                    FileInfo info = HACGUIKeyset.AccountsFolderInfo.GetFile($"{SaveUserId.ToString().ToLower()}.jpg");
                    if (info.Exists) {
                        JpegBitmapDecoder decoder = new JpegBitmapDecoder(info.OpenRead(), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        decoder.Frames[0].Freeze();
                        return decoder.Frames[0];
                    }
                    return ApplicationElement.UnknownIcon;
                }
                catch (Exception)
                {
                    return ApplicationElement.UnknownIcon;
                }
            });
        }

        private void CopyImage(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(new WriteableBitmap((BitmapSource)ProfileIcon.Source));
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
                    //mode = OpenMode.ReadWrite;
                    break;
            }

            MountService.Mount(new MountableFileSystem(Element.Save, view.ToString("x16"), "Savefile", mode));
        }

        private static bool IsWritable(IFileSystem save)
        {
            // ok so FUCK nintendo
            // their filesystem interface doesn't define RW privileges
            // so I have to guess lol
            IFile temp = null;
            try
            {
                save.CreateDirectory("/tmp".ToU8Span()).ThrowIfFailure();
                save.RenameDirectory("/tmp".ToU8Span(), "/temp".ToU8Span()).ThrowIfFailure();
                save.DeleteDirectory("/temp".ToU8Span()).ThrowIfFailure();
                save.CreateFile("/tmp.bin".ToU8Span(), 0, CreateFileOptions.None).ThrowIfFailure();
                save.OpenFile(out temp, "/tmp.bin".ToU8Span(), OpenMode.ReadWrite).ThrowIfFailure();
                temp.SetSize(0x4).ThrowIfFailure();
                byte[] testBytes = new byte[] { 0xBA, 0xDF, 0x00, 0xD };
                temp.Write(0, testBytes, WriteOption.None).ThrowIfFailure();
                byte[] buff = new byte[4];
                temp.Read(out long _, 0, buff, ReadOption.None).ThrowIfFailure();
                save.RenameFile("/tmp.bin".ToU8Span(), "/temp.bin".ToU8Span()).ThrowIfFailure();
                save.DeleteFile("/temp.bin".ToU8Span()).ThrowIfFailure();
                temp.Dispose();
                if (!buff.SequenceEqual(testBytes))
                    return false;
                return true;
            }
            catch (LibHac.LibHacException)
            {
                return false;
            }
            catch (NotImplementedException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            } 
            finally
            {
                if(temp != null)
                    temp.Dispose();
            }
        }

        public enum OpenType
        {
            ReadOnly, Writable
        }
    }
}
