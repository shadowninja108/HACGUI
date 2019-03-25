using LibHac;
using LibHac.IO.NcaUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors
{
    public abstract partial class IExtractorWindow : UserControl
    {
        protected List<Nca> SelectedNcas;

        public IExtractorWindow(List<Nca> selected)
        {
            SelectedNcas = selected;
        }
    }
}
