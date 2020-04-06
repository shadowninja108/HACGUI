using LibHac;
using LibHac.FsSystem.NcaUtils;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    public class NcaElement
    {
        public SwitchFsNca Nca;

        public string FileName => Nca.Filename;
        public long Size
        {
            get
            {
                Nca.Nca.BaseStorage.GetSize(out long size);
                return size;
            }
        }

        public NcaContentType Type => Nca.Nca.Header.ContentType;
        public bool Selected { get; set; }

    }
}
