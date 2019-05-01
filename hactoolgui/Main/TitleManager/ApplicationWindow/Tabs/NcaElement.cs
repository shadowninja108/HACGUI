using LibHac.IO.NcaUtils;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    public class NcaElement
    {
        public Nca Nca;

        public string FileName => Nca.Filename;
        public long Size => Nca.GetStorage().GetSize();
        public ContentType Type => Nca.Header.ContentType;
        public bool Selected { get; set; }

    }
}
