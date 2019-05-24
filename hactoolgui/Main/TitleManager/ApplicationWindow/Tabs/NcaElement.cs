using LibHac;
using LibHac.Fs.NcaUtils;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    public class NcaElement
    {
        public SwitchFsNca Nca;

        public string FileName => Nca.Filename;
        public long Size => Nca.Nca.BaseStorage.GetSize();
        public ContentType Type => Nca.Nca.Header.ContentType;
        public bool Selected { get; set; }

    }
}
