using LibHac;
using LibHac.IO.NcaUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
