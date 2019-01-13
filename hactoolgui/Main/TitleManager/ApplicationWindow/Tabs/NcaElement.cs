using LibHac;
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
        public long Size => Nca.GetStorage().Length;
        public ContentType Type => Nca.Header.ContentType;
        public bool Selected { get; set; }

    }
}
