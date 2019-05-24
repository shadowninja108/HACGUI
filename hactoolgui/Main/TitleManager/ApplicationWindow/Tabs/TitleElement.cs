using LibHac;
using System.Collections.Generic;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    public class TitleElement
    {
        public Title Title;

        public TitleType Type => Title.Metadata.Type;
        public ulong TitleId => Title.Id;
        public long Size => Title.GetSize();
        public bool Selected { get; set; }
        public List<NcaElement> Ncas
        {
            get
            {
                List<NcaElement> ncas = new List<NcaElement>();
                foreach (SwitchFsNca nca in Title.Ncas)
                    ncas.Add(new NcaElement() { Nca = nca });
                return ncas;
            }
        }
    }
}
