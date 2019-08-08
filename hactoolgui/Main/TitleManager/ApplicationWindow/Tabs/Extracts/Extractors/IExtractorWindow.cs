using LibHac;
using System.Collections.Generic;
using System.Windows.Controls;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors
{
    public abstract class IExtractorWindow : UserControl
    {
        protected List<SwitchFsNca> SelectedNcas;

        public IExtractorWindow()
        {
            if (!DesignMode.IsInDesignMode(this))
                throw new System.Exception();
        }

        public IExtractorWindow(List<SwitchFsNca> selected)
        {
            SelectedNcas = selected;
        }
    }
}
