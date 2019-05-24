using LibHac;
using System.Collections.Generic;
using System.Windows.Controls;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors
{
    public abstract partial class IExtractorWindow : UserControl
    {
        protected List<SwitchFsNca> SelectedNcas;

        public IExtractorWindow(List<SwitchFsNca> selected)
        {
            SelectedNcas = selected;
        }
    }
}
